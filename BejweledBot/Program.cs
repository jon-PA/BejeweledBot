using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using BejweledBot.Win32;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace BejweledBot
{
    static class Program
    {

        public class GemClass
        {
            public int ID { get; }
            public string FriendlyName { get; set; }
            public MCvScalar Color { get; set; }

            /// <summary>
            /// Can the gem be swapped with another?
            /// </summary>
            public bool Movable { get; set; } = true;

            /// <summary>
            /// If a gem is moved next to a group of these, will they be matched?
            /// </summary>
            public bool Matchable { get; set; } = true;

            public GemClass( int id, string friendlyName, MCvScalar color )
            {
                FriendlyName = friendlyName;
                ID           = id;
                Color        = color;
            }

            public override bool Equals( object? obj )
            {
                return obj is GemClass other && other.ID.Equals( this.ID );
            }

            public override int GetHashCode( ) => this.ID.GetHashCode( );

            public static List<GemClass> AllGems = new List<GemClass>
            {
                // This type should always exist, it should stay at fixed value 0. Ther will always be cases where the cell value is unknown.
                new GemClass( 0, "Unknown", new MCvScalar( 0,   0,   0 ) ) { Matchable = false, Movable = false },
                new GemClass( 1, "Red",     new MCvScalar( 208, 19,  41 ) ),
                new GemClass( 2, "Orange",  new MCvScalar( 228, 120, 37 ) ),
                new GemClass( 3, "Yellow",  new MCvScalar( 228, 196, 23 ) ),
                new GemClass( 4, "Green",   new MCvScalar( 28,  206, 59 ) ),
                new GemClass( 5, "Blue",    new MCvScalar( 13,  123, 220 ) ),
                new GemClass( 6, "Purple",  new MCvScalar( 202, 38,  196 ) ),
                new GemClass( 7, "White",   new MCvScalar( 202, 201, 201 ) )
            };
        }

        static GemClass RecognizeGemClass( MCvScalar srcColor )
        {
            GemClass closest = null;
            double closestDist = float.MaxValue;

            foreach( var gemType in GemClass.AllGems )
            {
                var diff = Math.Sqrt(
                    Math.Pow( Math.Abs( gemType.Color.V0 - srcColor.V0 ), 2 )
                  + Math.Pow( Math.Abs( gemType.Color.V1 - srcColor.V1 ), 2 )
                  + Math.Pow( Math.Abs( gemType.Color.V2 - srcColor.V2 ), 2 )
                );
                if( diff < closestDist )
                {
                    closestDist = diff;
                    closest     = gemType;
                }
            }

            return closest;
        }

        class MoveRule
        {
            public Point[] Offsets { get; set; }
            public Point SwapWith { get; set; } = new Point( 0, 1 );
            public int Priority { get; set; }
        }

        // All known moves where the direction is to swap the piece up
        static List<MoveRule> AllMoveRules = new List<MoveRule>
        {
            #region 5 Gem Combos

            // 5 Line
            new MoveRule
            {
                Offsets = new[] { new Point( -2, 1 ), new Point( -1, 1 ), new Point( 1, 1 ), new Point( 2, 1 ) },
            },
            // T Shape 
            new MoveRule
            {
                Offsets = new[] { new Point( 0, -2 ), new Point( 0, 3 ), new Point( -1, 1 ), new Point( 1, 1 ) },
            },

            #endregion 5 Gem Combos

            #region 4 Gem Combos

            // 4 In a line A
            new MoveRule
            {
                Offsets = new[] { new Point( -1, 1 ), new Point( 1, 1 ), new Point( 2, 1 ) },
            },
            // 4 In a line B
            new MoveRule
            {
                Offsets = new[] { new Point( -2, 1 ), new Point( -1, 1 ), new Point( 1, 1 ) },
            },

            #endregion 4 Gem Combos

            #region 3 Gem Combos

            new MoveRule
            {
                Offsets = new[] { new Point( 1, 1 ), new Point( 2, 1 ) },
            },
            new MoveRule
            {
                Offsets = new[] { new Point( -1, 1 ), new Point( -2, 1 ) },
            },
            new MoveRule
            {
                Offsets = new[] { new Point( 0, 2 ), new Point( 0, 3 ) },
            },
            new MoveRule
            {
                Offsets = new[] { new Point( -1, 1 ), new Point( 1, 1 ) },
            },

            #endregion 3 Gem Combos
        };

        class Move
        {
            public Point StartCell { get; set; }
            public Point TargetCell { get; set; }
            public List<Point> MatchedCells { get; set; }
            public MoveRule SourceRule { get; set; }
            
            // Perhaps poorly named. This will be non-null if this swap will match two seperate groups 
            public MoveRule TargetRule { get; set; } = null;
        }

        static void Main( string[] args )
        {
            GlobalKeyboardHook keyboardHook = new GlobalKeyboardHook( );

            keyboardHook.OnKeyPress += vk =>
            {
                // Console.WriteLine( vk );
                if( vk == VirtualKeyShort.KEY_Q )
                {
                    STOP_PROGRAM = true;
                    Console.WriteLine("Stop Signal");
                }
                else if( vk == VirtualKeyShort.KEY_G )
                {
                    AUTOPLAY = !AUTOPLAY;
                    Console.WriteLine( $"Autoplay: {AUTOPLAY}");
                }
                else if( vk == VirtualKeyShort.KEY_F )
                {
                    SINGLE_MOVE = true;
                    Console.WriteLine( "Single Move" );
                    
                }             
                else if( vk == VirtualKeyShort.KEY_B )
                {
                    NO_WAIT = !NO_WAIT;
                }
                else if( vk == VirtualKeyShort.KEY_R )
                {
                    CLEAR_BOUND = true;
                    Console.WriteLine($"Reset Key");

                }
                else if( vk == VirtualKeyShort.KEY_P )
                {
                    SHOW_PREVIEW_WINDOW = !SHOW_PREVIEW_WINDOW;
                    
                    Console.WriteLine($"Reset Key");

                }
                else if( vk == VirtualKeyShort.KEY_T )
                {
                    SET_BOUND = !SET_BOUND;
                    Console.WriteLine($"Set b Key {SET_BOUND}");

                    if( !SET_BOUND )
                    {
                        try
                        {
                            if( FirstPoint.HasValue && SecondPoint.HasValue )
                            {
                                File.WriteAllLines( LAST_BOUNDS_FILE_PATH, new string[]
                                {
                                    FirstPoint.Value.X.ToString( ),
                                    FirstPoint.Value.Y.ToString( ),
                                    SecondPoint.Value.X.ToString( ),
                                    SecondPoint.Value.Y.ToString( )
                                } );
                            }
                            else
                            {
                                File.Delete( LAST_BOUNDS_FILE_PATH );
                            }
                        }
                        catch( Exception e )
                        { }
                    }
                }
                else if( vk == VirtualKeyShort.ADD )
                    maskFillPercent = Math.Min( maskFillPercent + 0.05, 1 );
                else if( vk == VirtualKeyShort.SUBTRACT )
                    maskFillPercent = Math.Max( maskFillPercent - 0.05, 0.05 );
                else if( vk == VirtualKeyShort.NUMPAD6 )
                    targetGridCols = Math.Max( 0, targetGridCols + 1 );
                else if( vk == VirtualKeyShort.NUMPAD4 )
                    targetGridCols = Math.Max( 0, targetGridCols - 1 );
                else if( vk == VirtualKeyShort.NUMPAD8 )
                    targetGridRows = Math.Max( 0, targetGridRows + 1 );
                else if( vk == VirtualKeyShort.NUMPAD2 )
                    targetGridRows = Math.Max( 0, targetGridRows - 1 );
                else if( vk == VirtualKeyShort.KEY_V )
                    CLEAR_VISITED = true;
                // else if( vk == VirtualKeyShort.KEY_C )
                    // CAPTURE_UNRECOGNIZED_TILES = true;
            };

            new Thread( o => Run( ) ).Start( );

            while( !STOP_PROGRAM && CvInvoke.PollKey( ) == -1 )
            {
                Thread.Sleep( 10 );
            }

            keyboardHook.Dispose( );
        }

        static Rectangle BoundingBox( Point p1, Point p2 )
        {
            return new Rectangle(
                Math.Min( p1.X, p2.X ),
                Math.Min( p1.Y, p2.Y ),
                Math.Max( p1.X, p2.X ) - Math.Min( p1.X, p2.X ),
                Math.Max( p1.Y, p2.Y ) - Math.Min( p1.Y, p2.Y )
            );
        }

        static Rectangle BoundingBox( int x1, int y1, int x2, int y2 )
        {
            return new Rectangle(
                Math.Min( x1, x2 ),
                Math.Min( y1, y2 ),
                Math.Max( x1, x2 ) - Math.Min( x1, x2 ),
                Math.Max( y1, y2 ) - Math.Min( y1, y2 )
            );
        }

        static Point BoundToRect( Rectangle rectangle, Point point )
        {
            if( point.X < rectangle.Left )
                point.X = rectangle.Left;
            else if( point.X > rectangle.Right )
                point.X = rectangle.Right;
            if( point.Y < rectangle.Top )
                point.Y = rectangle.Top;
            else if( point.Y > rectangle.Bottom )
                point.Y = rectangle.Bottom;

            return point;
        }

        static int targetGridRows = 8;
        static int targetGridCols = 8;

        static double maskFillPercent = 0.5;

        static bool STOP_PROGRAM = false;
        static bool AUTOPLAY = false;
        static bool SINGLE_MOVE = false;
        static bool SET_BOUND = false;
        static bool CLEAR_BOUND = false;
        static bool NO_WAIT = false;
        static bool CLEAR_VISITED = false;
        static bool CAPTURE_UNRECOGNIZED_TILES = false;
        static bool SHOW_PREVIEW_WINDOW = false;

        static Point? FirstPoint = null;
        static Point? SecondPoint = null;
        const string LAST_BOUNDS_FILE_PATH = "last_bounds.txt";

        static void Run( )
        {
            // Add rotation permutations of defined moves
            foreach( var move in AllMoveRules.ToList( ) )
            {
                AllMoveRules.Add(
                    new MoveRule
                    {
                        Offsets  = move.Offsets.Select( p => new Point( p.Y, -p.X ) ).ToArray( ),
                        SwapWith = new Point( 1, 0 ),
                        Priority = move.Priority
                    }
                );
                AllMoveRules.Add(
                    new MoveRule
                    {
                        Offsets  = move.Offsets.Select( p => new Point( -p.X, -p.Y ) ).ToArray( ),
                        SwapWith = new Point( 0, -1 ),
                        Priority = move.Priority
                    }
                );
                AllMoveRules.Add(
                    new MoveRule
                    {
                        Offsets  = move.Offsets.Select( p => new Point( -p.Y, p.X ) ).ToArray( ),
                        SwapWith = new Point( -1, 0 ),
                        Priority = move.Priority
                    }
                );
            }

            foreach( var move in AllMoveRules )
            {
                move.Priority = move.Offsets.Length * 100;

                // 5 Points off for every offset on the X axis (clearing columns is better strategy)
                move.Priority -= move.Offsets.Sum( p => Math.Abs( p.X ) ) * 5;
            }


            // WindowCapture.TryCreateWindowCapture( "Bejeweled 3", out var capture );
            var screenCapture = new WindowCapture( IntPtr.Zero );

            Rectangle windowRect; // = screenCapture.GetWindowRect( );

            try
            {
                int gridRows = 8;
                int gridCols = 8;
                GemClass[,] board = new GemClass[gridRows, gridCols];

                Point cursorPos;
                Point windowCursorPos;

                int cellWidth, cellHeight;

                // Cells already part of a suspected match are added here so they are not interacted with until this is cleared
                HashSet<Point> visitedPoints = new HashSet<Point>( );

                var capturesDirectory = new DirectoryInfo( "captures" );
                if( !capturesDirectory.Exists ) capturesDirectory.Create( );

                if( File.Exists( LAST_BOUNDS_FILE_PATH ) )
                {
                    try
                    {
                        string[] lines = File.ReadAllLines( LAST_BOUNDS_FILE_PATH );
                        FirstPoint  = new Point( int.Parse( lines[0] ), int.Parse( lines[1] ) );
                        SecondPoint = new Point( int.Parse( lines[2] ), int.Parse( lines[3] ) );
                    }
                    catch( Exception e )
                    {
                        File.Delete( LAST_BOUNDS_FILE_PATH );
                    }
                }

                while(
                    !STOP_PROGRAM )
                {
                    CvInvoke.PollKey( );

                    // Copy this now so we don't read a changed value later down the line
                    var doShowPreviewWindow = SHOW_PREVIEW_WINDOW;

                    windowRect = screenCapture.GetWindowRect( );
                    var localWindowRect = new Rectangle( 0, 0, windowRect.Width, windowRect.Height );

                    NativeInterface.GetCursorPos( out var pnt );
                    cursorPos = pnt;
                    // Local space cursor bounded to the window
                    windowCursorPos = BoundToRect( localWindowRect,
                        new Point( cursorPos.X - windowRect.Left, cursorPos.Y - windowRect.Top ) );

                    if( CLEAR_BOUND )
                    {
                        try
                        {
                            File.Delete( LAST_BOUNDS_FILE_PATH );
                        }
                        catch( Exception e )
                        { }

                        FirstPoint  = null;
                        SecondPoint = null;

                        SET_BOUND   = false;
                        CLEAR_BOUND = false;

                        try
                        {
                            CvInvoke.DestroyWindow( "Preview" );
                            CvInvoke.DestroyWindow( "Set Start" );
                        }
                        catch
                        { }
                    }

                    if( CLEAR_VISITED )
                    {
                        visitedPoints.Clear( );
                        CLEAR_VISITED = false;
                    }

                    if( FirstPoint is null )
                    {
                        if( SET_BOUND )
                        {
                            FirstPoint = windowCursorPos;
                            try
                            {
                                CvInvoke.DestroyWindow( "Set Start" );
                            }
                            catch
                            { }
                        }
                    }

                    if( FirstPoint is null )
                        continue;

                    if( SET_BOUND )
                        SecondPoint = windowCursorPos;

                    var gridBB = BoundingBox( FirstPoint.Value, SecondPoint.Value ); // new Rectangle( 0, 0, 500, 500 );

                    if( gridBB.Width == 0 || gridBB.Height == 0 )
                        continue;

                    if( targetGridCols != gridCols || targetGridRows != gridRows )
                    {
                        gridRows = targetGridRows;
                        gridCols = targetGridCols;
                        board    = new GemClass[gridCols, gridRows];

                        visitedPoints.Clear( );
                    }

                    using var screenCap = screenCapture.Capture( gridBB );

                    cellWidth  = gridBB.Width / gridCols;
                    cellHeight = gridBB.Height / gridRows;

                    for( int y = 0; y < gridRows; y++ )
                    {
                        for( int x = 0; x < gridCols; x++ )
                        {
                            var cellX = x * cellWidth;
                            var cellY = y * cellHeight;

                            if( CAPTURE_UNRECOGNIZED_TILES )
                            {
                                screenCap.ROI = new Rectangle( cellX, cellY, cellWidth, cellHeight );
                                try
                                {
                                    screenCap.Save( capturesDirectory.FullName + $"\\{Guid.NewGuid( ):N}.png" );
                                }
                                catch
                                { }
                            }

                            var maskRectangle = new Rectangle(
                                cellX + (int)( ( cellWidth - (int)( cellWidth * maskFillPercent ) ) * 0.5 ),
                                cellY + (int)( ( cellHeight - (int)( cellHeight * maskFillPercent ) ) * 0.5 ),
                                (int)( cellWidth * maskFillPercent ),
                                (int)( cellHeight * maskFillPercent )
                            );
                            screenCap.ROI = maskRectangle;

                            var scalar = CvInvoke.Mean( screenCap );

                            var gemType = RecognizeGemClass( new MCvScalar( scalar.V2, scalar.V1, scalar.V0 ) );

                            if( doShowPreviewWindow )
                            {
                                screenCap.SetValue(
                                    new MCvScalar( gemType.Color.V2, gemType.Color.V1, gemType.Color.V0 ) );

                                screenCap.ROI = new Rectangle( );

                                CvInvoke.Rectangle( screenCap, new Rectangle( cellX, cellY, cellWidth, cellHeight ),
                                    new MCvScalar( 255, 255, 255 ), 1, LineType.FourConnected );

                            }
                            
                            // CvInvoke.PutText( preview, maxVal.ToString( ),
                            //     new Point( cellX + ( cellWidth / 2 ), cellY + ( cellHeight ) ),
                            //     FontFace.HersheyPlain, 5, new MCvScalar( 0, 0, 0 ), 3 );

                            board[x, y] = gemType;
                        }
                    }

                    if( CAPTURE_UNRECOGNIZED_TILES )
                        CAPTURE_UNRECOGNIZED_TILES = false;

                    if( doShowPreviewWindow )
                        CvInvoke.Imshow( "Preview", screenCap );
                    else if( CvInvoke.GetWindowProperty( "Preview", WindowPropertyFlags.Visible ) != 0 ) 
                        CvInvoke.DestroyWindow( "Preview" );
                    
                    // Wont do solving / movement if we are setting bounds
                    if( SET_BOUND )
                        continue;

                    (int X, int Y) GetScreenCell( int cellX, int cellY )
                    {
                        return (
                            windowRect.Left + gridBB.Left + cellX * cellWidth + ( cellWidth / 2 ),
                            windowRect.Top + gridBB.Top + cellY * cellHeight + ( cellHeight / 2 )
                        );
                    }

                    Point GridLocalCellPos( Point pt )
                    {
                        return new Point(
                            pt.X * cellWidth + ( cellWidth / 2 ),
                            pt.Y * cellHeight + ( cellHeight / 2 )
                        );
                    }

                    void ExecuteMove( Move move )
                    {
                        var (ssX, ssY) = GetScreenCell( move.StartCell.X,  move.StartCell.Y );
                        var (tsX, tsY) = GetScreenCell( move.TargetCell.X, move.TargetCell.Y );
                        
                        // Console.WriteLine($"MOVING {ssX}, {ssY} to {tsX}, {tsY} at ${DateTime.Now}");


                        Win32.NativeInterface.SetCursorPos( ssX, ssY );
                        Wrapper.SendMouseInputs( new MouseInput
                        {
                            ButtonAction = (uint)( MouseButtonAction.LeftDown ),
                            X = 0,
                            Y = 0,
                            ScrollAmount = 0
                        } );
                        // Thread.Sleep( 5 );
                        Win32.NativeInterface.SetCursorPos( tsX, tsY );
                        // Thread.Sleep( 3 );
                        Wrapper.SendMouseInputs( new MouseInput
                        {
                            ButtonAction = (uint)( MouseButtonAction.LeftUp ),
                            X            = 0,
                            Y            = 0,
                            ScrollAmount = 0
                        } );
                        
                    }

                    var validMoves = new List<Move>( );
                    foreach( var rule in AllMoveRules )
                    {
                        for( int y = 0; y < gridRows; y++ )
                        {
                            if( rule.Offsets.Any( o => y - o.Y < 0 || y - o.Y >= gridRows ) )
                                continue;

                            for( int x = 0; x < gridCols; x++ )
                            {
                                if( rule.Offsets.Any( o => x + o.X < 0 || x + o.X >= gridCols ) )
                                    continue;

                                var currentCellGem = board[x, y];
                                var currentCell = new Point( x, y );
                                var swapCell = new Point( currentCell.X + rule.SwapWith.X,
                                    currentCell.Y - rule.SwapWith.Y );

                                if( !currentCellGem.Movable || !currentCellGem.Matchable ||
                                    !board[swapCell.X, swapCell.Y].Movable )
                                    continue;

                                var checkMatchingCells =
                                    rule.Offsets.Select( p => new Point( x + p.X, y - p.Y ) ).ToList( );

                                if( checkMatchingCells.All( c => board[c.X, c.Y].Equals( currentCellGem ) ) )
                                {
                                    screenCap.DrawPolyline(
                                        checkMatchingCells.Prepend( currentCell ).Select( GridLocalCellPos ).ToArray( ),
                                        false,
                                        new Bgr(
                                            currentCellGem.Color.V2,
                                            currentCellGem.Color.V1,
                                            currentCellGem.Color.V0
                                        ),
                                        3,
                                        LineType.Filled
                                    );

                                    validMoves.Add( new Move
                                    {
                                        StartCell    = currentCell,
                                        TargetCell   = swapCell,
                                        MatchedCells = checkMatchingCells,
                                        SourceRule   = rule
                                    } );
                                }
                            }
                        }
                    }

                    List<Move> mergedMoves = new List<Move>( );
                    foreach( var move in validMoves )
                    {
                        if( mergedMoves.Contains( move ))
                            continue;
                        
                        var moveComplement = validMoves.FirstOrDefault( m => m.StartCell == move.TargetCell && m.TargetCell == move.StartCell );
                        if( moveComplement is not null )
                        {
                            move.TargetRule = moveComplement.SourceRule;
                            mergedMoves.Add( moveComplement );
                        }
                    }

                    // Remove moves that have been merged as a complement rule
                    foreach( var merged in mergedMoves )
                    {
                        validMoves.Remove( merged );
                        
                        // Console.WriteLine($"FOUND DOUBLE {merged.StartCell} - {merged.TargetCell}");
                        screenCap.DrawPolyline(
                            new Point[]{ GridLocalCellPos( merged.StartCell ), GridLocalCellPos( merged.TargetCell )},
                            false,
                            new Bgr(
                                255, 0, 255
                            ),
                            6,
                            LineType.FourConnected
                        );
                    }

                    if( doShowPreviewWindow)
                        CvInvoke.Imshow( "Preview", screenCap );

                    if( !AUTOPLAY && !SINGLE_MOVE )
                    {
                        Thread.Sleep( 5 );
                        continue;
                    }

                    foreach( var move in validMoves.ToList( ) )
                        if( visitedPoints.Contains( move.StartCell )
                         || visitedPoints.Contains( move.TargetCell )
                         || move.MatchedCells.Any( c => visitedPoints.Contains( c ) ) )
                            validMoves.Remove( move );

                    if( validMoves.Count > 0 )
                    {
                        Move chosenMove;
                        if( validMoves.Count == 1 )
                            chosenMove = validMoves[0];
                        else
                        {
                            chosenMove = validMoves.OrderByDescending( m => m.SourceRule.Priority + (m.TargetRule is not null ? m.TargetRule.Priority : 0) ).First( );
                        }

                        ExecuteMove( chosenMove );
                        visitedPoints.Add( chosenMove.StartCell );
                        visitedPoints.Add( chosenMove.TargetCell );
                        foreach( var affectedCell in chosenMove.MatchedCells )
                            visitedPoints.Add( affectedCell );

                        // Thread.Sleep( 500 );
                        if( validMoves.Count <= 3 && !NO_WAIT )
                            Thread.Sleep( 100 );
                    }
                    else
                    {
                        visitedPoints.Clear( );
                        if( !NO_WAIT ) 
                            Thread.Sleep( 350 );
                    }

                    SINGLE_MOVE = false;
                }
            }
            finally
            {
                screenCapture.Dispose( );
            }
        }
    }
}