using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using AForge.Video.VFW;
using AForge.Vision.Motion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MotionCapture
{
   
    public partial class MotionCCTV : Form
    {
        private VideoCaptureDevice cam;
        private MotionDetector md;
        public MotionCCTV()
        {
            
            InitializeComponent();
            isResolutionSet = false;
            webcam=new FilterInfoCollection(FilterCategory.VideoInputDevice);
            cam = new VideoCaptureDevice(webcam[0].MonikerString);
            md = new MotionDetector(new TwoFramesDifferenceDetector(), new MotionAreaHighlighting()); // creates the motion detector
            cam.NewFrame += new NewFrameEventHandler(cam_NewFrame); // defines which method to call when a new frame arrives
            cam.Start(); // starts the videoCapture
            Display.Paint += Display_Paint;
        }

        void Display_Paint(object sender, PaintEventArgs e)
        {

            using (Font myFont = new Font("Tahoma", 10, FontStyle.Bold))
            {

                e.Graphics.DrawString(DateTime.Now.ToString() + ((this.motionDetected) ? " + Motion !" : ""), myFont, ((this.motionDetected) ? Brushes.Red : Brushes.Green), new Point(2, 2));
                if (this.IsRecording)
                {
                    if (this.showRecordMarkerCount > 10)
                    {
                        e.Graphics.DrawString("[RECORDING]", myFont, Brushes.Red, new Point(2, 14));

                        if (this.showRecordMarkerCount == 20)
                        {
                            this.showRecordMarkerCount = 0;
                        }
                    }
                    this.showRecordMarkerCount++;
                }
            }
        }

        private void cam_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                Bitmap bit = (Bitmap)eventArgs.Frame.Clone(); // get a copy of the BitMap from the VideoCaptureDevice
                if (!this.isResolutionSet)
                {
                    // this is run once to set the resolution for the VideoRecorder
                    this.imgWidth = bit.Width;
                    this.imgHeight = bit.Height;
                    this.isResolutionSet = true;
                }
                this.Display.Image = (Bitmap)bit.Clone(); // displays the current frame on the main form

                if ( !this.motionDetected)
                {
                    // if motion detection is enabled and there werent any previous motion detected
                    Bitmap bit2 = (Bitmap)bit.Clone(); // clone the bits from the current frame

                    if (md.ProcessFrame(bit2) > 0.001) // feed the bits to the MD 
                    {
                        if (this.calibrateAndResume > 3)
                        {
                           
                            
                            Thread th = new Thread(MotionReaction);
                            th.Start(); // start the motion reaction thread
                        }
                        else this.calibrateAndResume++;
                    }

                }
                if (IsRecording)
                {
                    // if recording is enabled we enqueue the current frame to be encoded to a video file
                    Graphics gr = Graphics.FromImage(bit);
                    Pen p = new Pen(Color.Red);
                    p.Width = 5.0f;
                    using (Font myFont = new Font("Tahoma", 10, FontStyle.Bold))
                    {
                        gr.DrawString(DateTime.Now.ToString(), myFont, Brushes.Red, new Point(2, 2));
                    }
                    frames.Enqueue((Bitmap)bit.Clone());
                }

            }
            catch (InvalidOperationException ex) { }
        }

        private void MotionReaction(object obj)
        {

            this.motionDetected = true;
           
                this.StartRecording(); // record if Autorecord is toggled
           
                System.Console.Beep(400, 500);
                System.Console.Beep(800, 500);
                System.Console.Beep(600, 500);
                System.Console.Beep(800, 500);

            Thread.Sleep(10000); // the user is notified for 10 seconds
            calibrateAndResume = 0;
            this.motionDetected = false;
            Thread.Sleep(3000);
            
        }

        private void StartRecording()
        {
            if (!IsRecording)
            {
                // if were not already recording we start the recording thread
                this.IsRecording = true;
                Thread th = new Thread(DoRecord);
                th.Start();
            }
            
        }

        private void DoRecord(object obj)
        {
            AVIWriter writer = new AVIWriter();
            writer.Open( System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\video" + String.Format("{0:_dd-M-yyyy_hh-mm-ss}", DateTime.Now) + ".avi", this.imgWidth, this.imgHeight);
            writer.FrameRate = 15;
            // as long as we're recording
            // we dequeue the BitMaps waiting in the Queue and write them to the file
            while (IsRecording)
            {
                if (frames.Count > 0)
                {   
                    
                    Bitmap bmp = frames.Dequeue();
                    writer.AddFrame(bmp);
                }
            }
            writer.Close();
            
        }
        public void StopRecording()
        {
            this.IsRecording = false;
        }
        public bool isResolutionSet { get; set; }

        public int imgWidth { get; set; }

        public int imgHeight { get; set; }
        private FilterInfoCollection webcam;

        public bool IsRecording { get; set; }

        public bool motionDetected { get; set; }

        public int showRecordMarkerCount { get; set; }

        public int calibrateAndResume { get; set; }

        public bool MotionDetection { get; set; }
        public Queue<Bitmap> frames = new Queue<Bitmap>();

        private void MotionCCTV_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.cam.Stop();
            this.StopRecording();
        }

        private void MotionCCTV_Load(object sender, EventArgs e)
        {

        }
    }
}
