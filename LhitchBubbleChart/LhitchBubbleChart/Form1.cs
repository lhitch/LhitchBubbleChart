using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace LhitchBubbleChart
{
    public partial class BubChartDisplay : Form
    {
        float xMin, xMax = 0;
        float yMin, yMax = 0;
        float scale = 1;
        float epsilon = .1f;
        bool chartDrawn;

        List<CircleData> circleList;

        private bool ReadFile()
        {
            string filename = FileNameTxt.Text;
            if (!File.Exists(filename))
            {
                OutputLbl.Text = "File '" + filename + "' not found.";
                return false;
            }
            //read file line by line
            string dataline;
            char[] delims = { ',' };
            StreamReader filein = new StreamReader(filename);
            while((dataline = filein.ReadLine()) != null)
            {
                //add circle data to list, sorted largest to smallest
                string[] cirDat = dataline.Split(delims);
                CircleData entry = new CircleData(); ;
                entry.value = float.Parse(cirDat[0]);
                entry.name = cirDat[1];
                circleList.Add(entry);
            }
            //sort the list from largest to smallest values
            circleList = circleList.OrderByDescending(sort => sort.value).ToList();

            //test output
            //foreach(var i in circleList)
            //{
            //    OutputLbl.Text = OutputLbl.Text + i.value + " " + i.name + "; ";
            //}
            return true;
        }

        private float DistanceBetween(CircleData cir1, CircleData cir2)
        {
            float deltaX = cir2.positionX - cir1.positionX;
            float deltaY = cir2.positionY - cir1.positionY;

            return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        //FindCircleCircleIntersections is a function found at http://csharphelper.com/blog/2014/09/determine-where-two-circles-intersect-in-c/
        //it is used to find the possible locations of new circles using intersecting circles
        //the circles used are the radius of a large circle and the candidate circle,
        //and a second larger circle and the candidate circle
        //the two intersections indicate areas where the added radii of both circles would
        //indicate a circle center where the edges of the three circles would be in contact,
        //grouping the circles tightly together

        // Find the points where the two circles intersect.
        private int FindCircleCircleIntersections(
            float cx0, float cy0, float radius0,
            float cx1, float cy1, float radius1,
            out PointF intersection1, out PointF intersection2)
        {
            // Find the distance between the centers.
            float dx = cx0 - cx1;
            float dy = cy0 - cy1;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // See how many solutions there are.
            if (dist > radius0 + radius1)
            {
                // No solutions, the circles are too far apart.
                intersection1 = new PointF(float.NaN, float.NaN);
                intersection2 = new PointF(float.NaN, float.NaN);
                return 0;
            }
            else if (dist < Math.Abs(radius0 - radius1))
            {
                // No solutions, one circle contains the other.
                intersection1 = new PointF(float.NaN, float.NaN);
                intersection2 = new PointF(float.NaN, float.NaN);
                return 0;
            }
            else if ((dist == 0) && (radius0 == radius1))
            {
                // No solutions, the circles coincide.
                intersection1 = new PointF(float.NaN, float.NaN);
                intersection2 = new PointF(float.NaN, float.NaN);
                return 0;
            }
            else
            {
                // Find a and h.
                double a = (radius0 * radius0 -
                    radius1 * radius1 + dist * dist) / (2 * dist);
                double h = Math.Sqrt(radius0 * radius0 - a * a);

                // Find P2.
                double cx2 = cx0 + a * (cx1 - cx0) / dist;
                double cy2 = cy0 + a * (cy1 - cy0) / dist;

                // Get the points P3.
                intersection1 = new PointF(
                    (float)(cx2 + h * (cy1 - cy0) / dist),
                    (float)(cy2 - h * (cx1 - cx0) / dist));
                intersection2 = new PointF(
                    (float)(cx2 - h * (cy1 - cy0) / dist),
                    (float)(cy2 + h * (cx1 - cx0) / dist));

                // See if we have 1 or 2 solutions.
                if (dist == radius0 + radius1) return 1;
                return 2;
            }
        }

        private bool CheckCollisions(CircleData circle)
        {
            if(circle.hasPos == true)
            {
                foreach(CircleData i in circleList)
                {
                    //if the circle is placed and isnt the same circle
                    if(i.hasPos == true && i.name.CompareTo(circle.name) != 0)
                    {
                        if(DistanceBetween(i,circle) < i.value + circle.value - epsilon)
                        {
                            //collision
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void UpdateScaleBounds(CircleData cir)
        {
            //check to see if the circle exceeds current bounds
            xMin = Math.Min(xMin, cir.positionX - cir.value);
            xMax = Math.Max(xMax, cir.positionX + cir.value);
            yMin = Math.Min(yMin, cir.positionY - cir.value);
            yMax = Math.Max(yMax, cir.positionY + cir.value);
        }

        private void CreateChart()
        {
            int lastCirclePlaced = 0;
            int anchorCircle = 0;
            float distance;
            //place first circle in the center
            if(circleList.Count == 0)
            {
                return;
            }
            else
            {
                circleList[0].positionX = 0;
                circleList[0].positionY = 0;
                circleList[0].hasPos = true;
                UpdateScaleBounds(circleList[0]);
            }
            if(!(circleList.Count > 1))
            {
                return;
            }
            
            //go through the list
            for(int i = 1; i < circleList.Count; i++)
            {
                bool succeed;
                if (lastCirclePlaced == 0 && anchorCircle == 0)
                {
                    //place second triangle on the left edge of the first
                    distance = circleList[0].value + circleList[1].value;
                    circleList[i].positionX = circleList[0].positionX - distance;
                    circleList[i].positionY = circleList[0].positionY;
                    circleList[i].hasPos = true;
                    lastCirclePlaced = 1;
                    UpdateScaleBounds(circleList[i]);
                }
                else
                {
                    succeed = false;
                    do
                    {
                        //for each circle,find points where the circle with touch the anchor circle and the last circle placed
                        PointF intersect1, intersect2;

                        int intersections = FindCircleCircleIntersections(circleList[anchorCircle].positionX, circleList[anchorCircle].positionY, circleList[anchorCircle].value + circleList[i].value,
                            circleList[lastCirclePlaced].positionX, circleList[lastCirclePlaced].positionY, circleList[lastCirclePlaced].value + circleList[i].value,
                            out intersect1, out intersect2);

                        //check collision with circles
                        if (intersect1.X != float.NaN)
                        {
                            circleList[i].positionX = intersect1.X;
                            circleList[i].positionY = intersect1.Y;
                            circleList[i].hasPos = true;
                            if (CheckCollisions(circleList[i]))
                            {
                                succeed = true;
                                lastCirclePlaced = i;
                                UpdateScaleBounds(circleList[i]);
                            }
                            else
                            {
                                circleList[i].hasPos = false;
                            }
                        }
                        if(succeed == false && intersect2.X != float.NaN)
                        {
                            circleList[i].positionX = intersect2.X;
                            circleList[i].positionY = intersect2.Y;
                            circleList[i].hasPos = true;
                            if (CheckCollisions(circleList[i]))
                            {
                                succeed = true;
                                lastCirclePlaced = i;
                                UpdateScaleBounds(circleList[i]);
                            }
                            else
                            {
                                circleList[i].hasPos = false;
                            }
                        }
                        //move anchor to next circle if no room found
                        if (succeed == false || intersections == 0)
                        {
                            do
                            {
                                anchorCircle++;
                                lastCirclePlaced = anchorCircle + 1;
                                //if the new anchor and the circle that was placed after it are not next to each other
                                //pick a new anchor
                            } while (!((DistanceBetween(circleList[anchorCircle], circleList[lastCirclePlaced]) - epsilon) > (circleList[anchorCircle].value + circleList[lastCirclePlaced].value)));
                        }
                    } while (succeed == false);
                }
                UpdateScaleBounds(circleList[i]);
            }
        }

        private void DrawDebug(CircleData c1,CircleData c2)
        {
            float originX = this.Width / 2;
            float originY = this.Height / 2;
            Pen blackPen = new Pen(Color.Black, 2);
            Graphics graph = this.CreateGraphics();
            float radius = c1.value * scale;
            float posX = c1.positionX * scale - radius + originX;
            float posY = c1.positionY * scale - radius + originY;
            graph.DrawEllipse(blackPen, posX, posY, radius * 2, radius * 2);
            radius = c2.value * scale;
            posX = c2.positionX * scale - radius + originX;
            posY = c2.positionY * scale - radius + originY;
            graph.DrawEllipse(blackPen, posX, posY, radius * 2, radius * 2);
            graph.Dispose();
            blackPen.Dispose();
        }

        //Draw a packed bubble chart using the chart stored in circlelist.
        private void DrawChart()
        {
            // drawing code influenced from this video https://www.youtube.com/watch?v=6KzwO07qa90
            // and this video https://www.youtube.com/watch?v=pV0M-za-7eI
            // text drawing code influenced by https://docs.microsoft.com/en-us/dotnet/framework/winforms/advanced/how-to-draw-text-on-a-windows-form

            Graphics graph = this.CreateGraphics();
            graph.Clear(SystemColors.Control);
            float originX = this.Width / 2;
            float originY = this.Height / 2;

            float formBounds = Math.Min(this.Height, this.Width);
            float bounds = Math.Max((xMax - xMin), (yMax - yMin));


            //create a scale factor based off the size of the chart and the size of the form
            scale = formBounds / bounds;
            //originX += (xMax -xMin) / scale;
            //originY -= (yMax - yMin) /scale;

            //circle border
            Pen blackPen = new Pen(Color.Black,2);
            //circle fill
            SolidBrush fillblue = new SolidBrush(Color.Cyan);
            //text
            Font drawingFont = new Font("Arial", 10);
            //text color
            SolidBrush textBlack = new SolidBrush(Color.Black);

            foreach(CircleData i in circleList)
            {
                //only draw circle that were successfully placed
                if (i.hasPos)
                {
                    float radius = i.value * scale;
                    float posX = i.positionX * scale - radius + originX;
                    float posY = i.positionY * scale - radius + originY;
                    string cirText = i.name + "\n" + i.value.ToString();
                    //draw the circle at position, with origin offset to the center of the form
                    graph.DrawEllipse(blackPen, posX, posY, radius*2, radius* 2);
                    graph.FillEllipse(fillblue, posX, posY, radius*2, radius* 2);
                    //find size of the string  to get offsets to center in the circle
                    SizeF nameSize = new SizeF();
                    nameSize = graph.MeasureString(cirText, drawingFont);
                    graph.DrawString(cirText, drawingFont, textBlack,(posX + radius - (nameSize.Width / 2)), (posY + radius - (nameSize.Height / 2)));
                }
            }
            //free up drawing tools
            textBlack.Dispose();
            drawingFont.Dispose();
            blackPen.Dispose();
            fillblue.Dispose();
            graph.Dispose();

            chartDrawn = true;
        }

        private void BubChartDisplay_Resize(object sender, EventArgs e)
        {
            //redraws chart if form resized with a chart drawn
            if(chartDrawn)
            {
                DrawChart();
            }
        }

        public BubChartDisplay()
        {
            chartDrawn = false;
            InitializeComponent();
        }

        private void GenerateBtn_Click(object sender, EventArgs e)
        {
            circleList = new List<CircleData>();
            OutputLbl.Text = "";
            //read file and compile list
            if(ReadFile() == false)
            {
                return;
            }
            //iterate through list and create chart
            CreateChart();
            //draw chart
            DrawChart();
        }
    }
}
