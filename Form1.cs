using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Drop3DForm
{
    public partial class Form1 : Form
    {
        private float alpha, beta, gamma, scale;
        private float moveX, moveY, moveZ;
        private Timer timer;

        // Переменные для включения/выключения вращения
        private bool rotateAlpha = true;
        private bool rotateBeta = true;
        private bool rotateGamma = false;

        public Form1()
        {
            this.Text = "3D Surface - Drop 1";
            this.Width = 800;
            this.Height = 600;
            this.DoubleBuffered = true;

            alpha = 0;
            beta = 0;
            gamma = 0;
            scale = 10;
            moveX = 0;
            moveY = 0;
            moveZ = 0;

            this.KeyDown += new KeyEventHandler(OnKeyDown);
            this.Paint += new PaintEventHandler(OnPaint);

            // Добавление текстовой подсказки
            Label instructionsLabel = new Label();
            instructionsLabel.Text = "Управление:\r\n" +
                "- Стрелки: Перемещение вдоль X и Y\r\n" +
                "- W / S: Перемещение вдоль Z\r\n" +
                "-+ / -: Масштабирование\r\n" +
                "- F1: Включение / выключение вращения вокруг оси X\r\n" +
                "-F2: Включение / выключение вращения вокруг оси Y\r\n" +
                "-F3: Включение / выключение вращения вокруг оси Z\r\n";
            instructionsLabel.AutoSize = true;
            instructionsLabel.Location = new Point(10, 10);
            instructionsLabel.BackColor = Color.White;
            this.Controls.Add(instructionsLabel);

            timer = new Timer();
            timer.Interval = 30;
            timer.Tick += (s, e) => {
                if (rotateAlpha) alpha += 0.02f;
                if (rotateBeta) beta += 0.02f;
                if (rotateGamma) gamma += 0.02f;
                Invalidate();
            };
            timer.Start();
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            List<PointF> points = new List<PointF>();
            List<(PointF, PointF)> lines = new List<(PointF, PointF)>();
            List<List<PointF>> polygons = new List<List<PointF>>();

            float R = scale;
            PointF? prevPoint = null;
            List<PointF> currentPolygon = new List<PointF>();

            for (float alpha = 0; alpha <= (float)Math.PI; alpha += 0.1f)
            {
                prevPoint = null;
                currentPolygon.Clear();

                for (float beta = 0; beta <= 2 * (float)Math.PI; beta += 0.1f)
                {
                    float x = R * (float)Math.Sin(alpha) * (float)Math.Cos(beta);
                    float y = R * (float)Math.Sin(alpha) * (float)Math.Sin(beta);
                    float z;

                    if (alpha <= (float)Math.PI / 3)
                    {
                        z = R * (float)Math.Cos(alpha) / (float)Math.Cos((float)Math.PI / 3 - alpha);
                    }
                    else
                    {
                        z = R * (float)Math.Cos(alpha);
                    }

                    // Apply transformation
                    float[] vector = { x, y, z, 1 };
                    vector = ApplyTransformations(vector);

                    // Project to 2D
                    PointF point = ProjectTo2D(vector);
                    points.Add(point);
                    currentPolygon.Add(point);

                    // Draw lines between consecutive points in the same row
                    if (prevPoint != null)
                    {
                        lines.Add((prevPoint.Value, point));
                    }

                    prevPoint = point;
                }

                // Store the current polygon (row of points)
                if (currentPolygon.Count > 1)
                {
                    polygons.Add(new List<PointF>(currentPolygon));
                }
            }

            // Draw points
            foreach (var point in points)
            {
                g.FillEllipse(Brushes.Black, point.X, point.Y, 2, 2);
            }

            // Draw lines
            foreach (var line in lines)
            {
                g.DrawLine(Pens.Gray, line.Item1, line.Item2);
            }

            // Draw polygons
            foreach (var polygon in polygons)
            {
                if (polygon.Count > 2)
                {
                    g.FillPolygon(new SolidBrush(Color.FromArgb(50, Color.Blue)), polygon.ToArray());
                    g.DrawPolygon(Pens.Blue, polygon.ToArray());
                }
            }
        }

        private float[] ApplyTransformations(float[] vector)
        {
            float[,] transformMatrix = GetTransformationMatrix();
            float[] result = new float[4];

            for (int i = 0; i < 4; i++)
            {
                result[i] = 0;
                for (int j = 0; j < 4; j++)
                {
                    result[i] += transformMatrix[i, j] * vector[j];
                }
            }

            return result;
        }

        private float[,] GetTransformationMatrix()
        {
            float cosAlpha = (float)Math.Cos(alpha);
            float sinAlpha = (float)Math.Sin(alpha);
            float cosBeta = (float)Math.Cos(beta);
            float sinBeta = (float)Math.Sin(beta);
            float cosGamma = (float)Math.Cos(gamma);
            float sinGamma = (float)Math.Sin(gamma);

            return new float[,]
            {
                { cosBeta * cosGamma, -sinBeta, cosBeta * sinGamma, moveX },
                { sinAlpha * sinBeta * cosGamma + cosAlpha * sinGamma, cosAlpha * cosBeta, sinAlpha * sinBeta * sinGamma - cosAlpha * cosGamma, moveY },
                { cosAlpha * sinBeta * cosGamma - sinAlpha * sinGamma, sinAlpha * cosBeta, cosAlpha * sinBeta * sinGamma + sinAlpha * cosGamma, moveZ },
                { 0, 0, 0, 1 }
            };
        }

        private PointF ProjectTo2D(float[] vector)
        {
            float perspective = 800 / (800 - vector[2]);
            float x = vector[0] * perspective + Width / 2;
            float y = -vector[1] * perspective + Height / 2;
            return new PointF(x, y);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    moveY -= 10;
                    break;
                case Keys.Down:
                    moveY += 10;
                    break;
                case Keys.Left:
                    moveX -= 10;
                    break;
                case Keys.Right:
                    moveX += 10;
                    break;
                case Keys.W:
                    moveZ += 10;
                    break;
                case Keys.S:
                    moveZ -= 10;
                    break;
                case Keys.Oemplus:
                    scale += 1;
                    break;
                case Keys.OemMinus:
                    scale -= 1;
                    break;
                case Keys.A:
                    gamma -= 0.1f;
                    break;
                case Keys.D:
                    gamma += 0.1f;
                    break;
                case Keys.F1:
                    rotateAlpha = !rotateAlpha;
                    break;
                case Keys.F2:
                    rotateBeta = !rotateBeta;
                    break;
                case Keys.F3:
                    rotateGamma = !rotateGamma;
                    break;
            }
            Invalidate();
        }

        [STAThread]
        public static void Main()
        {
            Application.Run(new Form1());
        }
    }
}
