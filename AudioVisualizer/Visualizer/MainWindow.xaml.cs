using AudioVisualizer.AudioAnalyzerV2;
using AudioVisualizer.AudioAnalyzerV3Chatgpt;
using System.Drawing;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Visualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //AudioAnalyzerV2 analyzer;
        AudioAnalyzerV3Chatgpt analyzer;
        public string time { get; set; }
        public int Magnification { get; set; } = 800;
        int oneBlockWidth => (int)Math.Round(ActualWidth / (analyzer.AmountOfBins - 6));//CHANGE THIS FOR ZOOM
        Canvas canvasEL;
        List <System.Windows.Shapes.Rectangle> sparkles = new List<System.Windows.Shapes.Rectangle>();
        List<System.Windows.Shapes.Rectangle> visualizerRectangles = new List<System.Windows.Shapes.Rectangle>();

        List<System.Windows.Shapes.Rectangle> decayRectangles = new List<System.Windows.Shapes.Rectangle>();
        Path BezierCurve = new Path();
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            analyzer = new AudioAnalyzerV3Chatgpt();
            //analyzer = new AudioAnalyzerV2();

            analyzer.AmountOfBins = 32 * 1;
            StartVisualizer();
            //StartDebugger();
            //analyzer.Stop();


        }
        public async void StartVisualizer()
        {
            canvasEL = new Canvas();
            DockPanel.SetDock(canvasEL, Dock.Bottom);
            DockPanelEl.Children.Add(canvasEL);

            AlertDot.Fill = System.Windows.Media.Brushes.Green;

            canvasEL.Children.Add(BezierCurve);

            analyzer.Start();            
            for (int i = 0; i < analyzer.AmountOfBins - 6; i++)//CHANGE THIS FOR ZOOM
            {
                //TextBlock tx = new TextBlock();
                //spEl.Children.Add(tx);
                System.Windows.Shapes.Rectangle rc = new System.Windows.Shapes.Rectangle();
                rc.Width = oneBlockWidth;
                rc.Fill = System.Windows.Media.Brushes.MediumPurple;
                rc.Stroke = System.Windows.Media.Brushes.AliceBlue;
                Canvas.SetLeft(rc, rc.Width * i);
                Canvas.SetBottom(rc, 0);
                Canvas.SetZIndex(rc, 1);
                visualizerRectangles.Add(rc);

                System.Windows.Shapes.Rectangle rcDecay = new System.Windows.Shapes.Rectangle();
                rcDecay.Width = oneBlockWidth;
                rcDecay.Height = 10;
                rcDecay.Fill = System.Windows.Media.Brushes.Purple ;
                rcDecay.Stroke = System.Windows.Media.Brushes.AliceBlue;
                Canvas.SetLeft(rcDecay, rcDecay.Width * i);
                Canvas.SetBottom(rcDecay, 0);
                Canvas.SetZIndex(rcDecay, 0);
                decayRectangles.Add(rcDecay);

            }
            while (analyzer.AnalyzerRunning)
            {
                float[] allFreq = analyzer.FFT();
                //time = DateTime.Now.ToLongTimeString();
                TIMER.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                for (int i = 0; i < visualizerRectangles.Count(); i++)
                {
                    System.Windows.Shapes.Rectangle visualizerRectangle = (System.Windows.Shapes.Rectangle)visualizerRectangles[i];
                    //double prevHeight = visualizerRectangle.Height;
                    visualizerRectangle.Height = Math.Round(allFreq[i] * Magnification);
                    visualizerRectangle.Width = oneBlockWidth;

                    Canvas.SetLeft(visualizerRectangle, visualizerRectangle.Width * i);
                    Canvas.SetBottom(visualizerRectangle, 0);

                    System.Windows.Shapes.Rectangle decayRectangle = (System.Windows.Shapes.Rectangle)decayRectangles[i];
                    decayRectangle.Width = oneBlockWidth;
                    Canvas.SetLeft(decayRectangle, decayRectangle.Width * i);

                    if (visualizerRectangle.Height > Canvas.GetBottom(decayRectangle))
                    {
                        if (visualizerRectangle.Height < ActualHeight)
                        {
                            Canvas.SetBottom(decayRectangle, visualizerRectangle.Height);
                        }
                        else
                        {
                            Canvas.SetBottom(decayRectangle, ActualHeight - decayRectangle.Height);
                        }                        
                    }


                    //ls.Width = Math.Round(allFreq[i] * 5000);

                    //SPARKLES
                    //if (visualizerRectangle.Height > 50)
                    //{
                    //    for (int x = 0; x < 2; x++)
                    //    {
                    //        System.Windows.Shapes.Rectangle rcSparkle = new System.Windows.Shapes.Rectangle();
                    //        rcSparkle.Fill = GetRandomColor();
                    //        rcSparkle.Width = 3;
                    //        rcSparkle.Height = 3;
                    //        Canvas.SetTop(rcSparkle, Random.Shared.Next((int)Math.Round(ActualHeight)));
                    //        Canvas.SetLeft(rcSparkle, Random.Shared.Next((int)Math.Round(ActualWidth)));

                    //        //sparkles.Add(rcSparkle);
                    //    }                        
                    //}
                    //else
                    //{
                    //    if (Random.Shared.Next(2) == 1 && sparkles.Count() != 0)
                    //    {
                    //        canvasEL.Children.Remove(sparkles[sparkles.Count() - 1]);
                    //        sparkles.RemoveAt(sparkles.Count() - 1);
                    //    }
                    //}

                    //ls.Text = Random.Shared.Next().ToString();
                    DebugText.Text = ActualWidth.ToString() + " " + oneBlockWidth;    
                }
                CalculateDecay();
                Display();
                //DebugText.Text = sparkles.Count().ToString();
                //textblock1.Text = Convert.ToString(analyzer.writeHead);
                await Task.Delay(1);
            }
            
        }

        int maxDescent = 1;
        int rateOfDescent = 0;
        public void CalculateDecay(){
            if (rateOfDescent == 0)
            {
                foreach (System.Windows.Shapes.Rectangle rectEl in decayRectangles)
                {
                    if (Canvas.GetBottom(rectEl) != 0)  Canvas.SetBottom(rectEl, Canvas.GetBottom(rectEl) - 1);
                }
            }
            if (rateOfDescent >= maxDescent) rateOfDescent = 0; else rateOfDescent++;
        }

        public System.Windows.Media.Brush GetRandomColor()
        {
            switch (Random.Shared.Next(1,6)) 
            {
                case 1:
                    return System.Windows.Media.Brushes.Purple;
                case 2:
                    return System.Windows.Media.Brushes.MediumPurple;
                case 3:
                    return System.Windows.Media.Brushes.Gold;
                case 4:
                    return System.Windows.Media.Brushes.Yellow;
                case 5:
                    return System.Windows.Media.Brushes.DarkRed;
                default:
                    return System.Windows.Media.Brushes.White;
            }

        }
        public void Display()
        {
            foreach (var sparkle in sparkles)
            {
                if (!canvasEL.Children.Contains(sparkle))
                {
                    canvasEL.Children.Add(sparkle);
                }
            }
            foreach (var rectangle in visualizerRectangles)
            {
                if (!canvasEL.Children.Contains(rectangle))
                {
                    canvasEL.Children.Add(rectangle);
                }
            }
            foreach (var rectangle in decayRectangles)
            {
                if (!canvasEL.Children.Contains(rectangle))
                {
                    canvasEL.Children.Add(rectangle);
                }
            }
            BezierCurve = MakeBezierPath(decayRectangles.Select(x => new System.Windows.Point(Canvas.GetBottom(x), Canvas.GetLeft(x))).ToArray());
            BezierCurve.Fill = System.Windows.Media.Brushes.Red;            
            BezierCurve.Stroke = System.Windows.Media.Brushes.Red;
            BezierCurve.StrokeThickness = 2;

        }

        public async void StartDebugger()
        {
            analyzer.Start();
            AlertDot.Fill = System.Windows.Media.Brushes.Blue;
            WrapPanel wpEl = new WrapPanel();
            for (int i = 0; i < analyzer.CircularFloatBuffer.Length; i++)
            {
                System.Windows.Shapes.Rectangle rcEl = new System.Windows.Shapes.Rectangle();
                rcEl.Width = 10;
                rcEl.Height = 10;
                if (analyzer.CircularFloatBuffer[i] == 0)
                {
                    rcEl.Fill = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    rcEl.Fill = System.Windows.Media.Brushes.Blue;
                }

                wpEl.Children.Add(rcEl);
            }
            DockPanelEl.Children.Add(wpEl);
            while (true)
            {
                TIMER.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                for (int i = 0; i < analyzer.CircularFloatBuffer.Length; i++)
                {
                    System.Windows.Shapes.Rectangle rc = (System.Windows.Shapes.Rectangle)wpEl.Children[i];
                    if (analyzer.CircularFloatBuffer[i] == 0)
                    {
                        rc.Fill = System.Windows.Media.Brushes.Red;
                    }
                    else
                    {
                        rc.Fill = System.Windows.Media.Brushes.Blue;
                    }
                    DebugText.Text = i.ToString();
                }
                await Task.Delay(10);
            }           

        }
        public void deleteSparkle(object? sender, EventArgs e)
        {

        }

        private Path MakeBezierPath(System.Windows.Point[] points)
        {
            // Create a Path to hold the geometry.
            Path path = new Path();

            // Add a PathGeometry.
            PathGeometry path_geometry = new PathGeometry();
            path.Data = path_geometry;

            // Create a PathFigure.
            PathFigure path_figure = new PathFigure();
            path_geometry.Figures.Add(path_figure);

            // Start at the first point.
            path_figure.StartPoint = points[0];

            // Create a PathSegmentCollection.
            PathSegmentCollection path_segment_collection =
                new PathSegmentCollection();
            path_figure.Segments = path_segment_collection;

            // Add the rest of the points to a PointCollection.
            PointCollection point_collection =
                new PointCollection(points.Length - 1);
            for (int i = 1; i < points.Length; i++)
                point_collection.Add(points[i]);

            // Make a PolyBezierSegment from the points.
            PolyBezierSegment bezier_segment = new PolyBezierSegment();
            bezier_segment.Points = point_collection;

            // Add the PolyBezierSegment to othe segment collection.
            path_segment_collection.Add(bezier_segment);

            return path;
        }
    }

    public class Sparkle
    {
        //private System.Windows.Shapes.Rectangle rc { get; set; } = new System.Windows.Shapes.Rectangle();
        //private Timer decayTime;
        //public event EventHandler? Expired;
        //public Sparkle(System.Windows.Media.Brush color, int height, int width, TimeSpan decayTime, EventHandler? expired)
        //{
        //    rc.Fill = color;
        //    rc.Height = height;
        //    rc.Width = width;
        //    decayTime += expired;
        //}        
        
    }
}