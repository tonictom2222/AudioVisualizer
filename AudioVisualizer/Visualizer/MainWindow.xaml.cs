using AudioVisualizer.AudioAnalyzerV2;
using AudioVisualizer.AudioAnalyzerV3Chatgpt;
using System.Drawing;
using System.Globalization;
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
        public int Magnification { get; set; } = 30000;
        Canvas canvasEL;
        List <System.Windows.Shapes.Rectangle> sparkles = new List<System.Windows.Shapes.Rectangle>();
        List<System.Windows.Shapes.Rectangle> visualizerRectangles = new List<System.Windows.Shapes.Rectangle>();
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            analyzer = new AudioAnalyzerV3Chatgpt();
            //analyzer = new AudioAnalyzerV2();

            analyzer.AmountOfBins = 32 * 4;
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

            analyzer.Start();            
            for (int i = 0; i < analyzer.AmountOfBins / 2; i++)//CHANGE THIS FOR ZOOM
            {
                //TextBlock tx = new TextBlock();
                //spEl.Children.Add(tx);
                System.Windows.Shapes.Rectangle rc = new System.Windows.Shapes.Rectangle();
                rc.Width = (int)Math.Round(Width / (analyzer.AmountOfBins / 2));//CHANGE THIS FOR ZOOM
                rc.Fill = System.Windows.Media.Brushes.Red;
                Canvas.SetLeft(rc, rc.Width * i);
                Canvas.SetBottom(rc, 0);
                Canvas.SetZIndex(rc, 1);
                visualizerRectangles.Add(rc);                
            }
            while (analyzer.AnalyzerRunning)
            {
                float[] allFreq = analyzer.FFT();
                //time = DateTime.Now.ToLongTimeString();
                TIMER.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                for (int i = 0; i < visualizerRectangles.Count(); i++)
                {
                    System.Windows.Shapes.Rectangle visualizerRectangle = (System.Windows.Shapes.Rectangle)visualizerRectangles[i];
                    double prevHeight = visualizerRectangle.Height;
                    visualizerRectangle.Height = Math.Round(allFreq[i] * Magnification);
                    //ls.Width = Math.Round(allFreq[i] * 5000);

                    //SPARKLES
                    if (visualizerRectangle.Height > 50)
                    {
                        for (int x = 0; x < 2; x++)
                        {
                            System.Windows.Shapes.Rectangle rcSparkle = new System.Windows.Shapes.Rectangle();
                            rcSparkle.Fill = GetRandomColor();
                            rcSparkle.Width = 3;
                            rcSparkle.Height = 3;
                            Canvas.SetTop(rcSparkle, Random.Shared.Next((int)Math.Round(ActualHeight)));
                            Canvas.SetLeft(rcSparkle, Random.Shared.Next((int)Math.Round(ActualWidth)));

                            sparkles.Add(rcSparkle);
                        }                        
                    }
                    //else
                    //{
                    //    if (Random.Shared.Next(2) == 1 && sparkles.Count() != 0)
                    //    {
                    //        canvasEL.Children.Remove(sparkles[sparkles.Count() - 1]);
                    //        sparkles.RemoveAt(sparkles.Count() - 1);
                    //    }
                    //}

                    //ls.Text = Random.Shared.Next().ToString();
                }
                Display();
                DebugText.Text = sparkles.Count().ToString();
                //textblock1.Text = Convert.ToString(analyzer.writeHead);
                await Task.Delay(1);
            }
            
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
            foreach(var rectangle in visualizerRectangles)
            {
                if (!canvasEL.Children.Contains(rectangle))
                {
                    canvasEL.Children.Add(rectangle);
                }
            }
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
    }
    public class Sparkle
    {
        private System.Windows.Shapes.Rectangle rc { get; set; } = new System.Windows.Shapes.Rectangle();
        private Timer decayTime;
        public event EventHandler? Expired;
        public Sparkle(System.Windows.Media.Brush color, int height, int width, TimeSpan decayTime, EventHandler? expired)
        {
            rc.Fill = color;
            rc.Height = height;
            rc.Width = width;
            decayTime += expired;
        }        
    }
}