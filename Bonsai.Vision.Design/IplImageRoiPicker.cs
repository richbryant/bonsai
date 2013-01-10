﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCV.Net;
using System.Reactive.Linq;
using System.Windows.Forms;
using System.Reactive;
using Bonsai.Vision.Design;
using System.Collections.ObjectModel;
using System.Drawing;
using Bonsai.Vision;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace Bonsai.Vision.Design
{
    class IplImageRoiPicker : IplImageControl
    {
        int? selectedRoi;
        Collection<CvPoint[]> regions;
        const float LineWidth = 1;
        const float PointSize = 2;

        public IplImageRoiPicker()
        {
            regions = new Collection<CvPoint[]>();

            this.Canvas.KeyDown += new KeyEventHandler(PictureBox_KeyDown);
            var mouseDoubleClick = Observable.FromEventPattern<MouseEventArgs>(Canvas, "MouseDoubleClick").Select(e => e.EventArgs);
            var mouseMove = Observable.FromEventPattern<MouseEventArgs>(Canvas, "MouseMove").Select(e => e.EventArgs);
            var mouseDown = Observable.FromEventPattern<MouseEventArgs>(Canvas, "MouseDown").Select(e => e.EventArgs);
            var mouseUp = Observable.FromEventPattern<MouseEventArgs>(Canvas, "MouseUp").Select(e => e.EventArgs);
            
            var roiSelected = from downEvt in mouseDown
                              let location = NormalizedLocation(downEvt.X, downEvt.Y)
                              let selection = ModifierKeys.HasFlag(Keys.Control) ? null :
                                              (from region in regions.Select((polygon, i) => new { polygon, i = (int?)i })
                                               let distance = TestIntersection(region.polygon, location)
                                               where distance > 0
                                               orderby distance
                                               select region.i)
                                               .FirstOrDefault()
                              select selection;

            var roiMove = from downEvt in mouseDown
                          where downEvt.Button == MouseButtons.Left && selectedRoi.HasValue
                          let location = NormalizedLocation(downEvt.X, downEvt.Y)
                          let region = regions[selectedRoi.Value]
                          from moveEvt in mouseMove.TakeUntil(mouseUp)
                          let target = NormalizedLocation(moveEvt.X, moveEvt.Y)
                          let displacement = target - location
                          select new Action(() => regions[selectedRoi.Value] = region.Select(point => point + displacement).ToArray());

            var pointMove = from downEvt in mouseDown
                            where downEvt.Button == MouseButtons.Right && selectedRoi.HasValue
                            let location = NormalizedLocation(downEvt.X, downEvt.Y)
                            let region = regions[selectedRoi.Value]
                            let nearestPoint = NearestPoint(region, location)
                            from moveEvt in mouseMove.TakeUntil(mouseUp)
                            let target = NormalizedLocation(moveEvt.X, moveEvt.Y)
                            select new Action(() => regions[selectedRoi.Value][nearestPoint] = target);

            var regionInsertion = from downEvt in mouseDown
                                  where downEvt.Button == MouseButtons.Left && !selectedRoi.HasValue
                                  let origin = NormalizedLocation(downEvt.X, downEvt.Y)
                                  from moveEvt in mouseMove.TakeUntil(mouseUp)
                                  let location = NormalizedLocation(moveEvt.X, moveEvt.Y)
                                  select new[]
                                  {
                                      origin,
                                      new CvPoint(location.X, origin.Y),
                                      location,
                                      new CvPoint(origin.X, location.Y)
                                  };

            var pointInsertion = from clickEvt in mouseDoubleClick
                                 where clickEvt.Button == MouseButtons.Left && selectedRoi.HasValue
                                 let location = NormalizedLocation(clickEvt.X, clickEvt.Y)
                                 let region = regions[selectedRoi.Value]
                                 let nearestPoint = NearestPoint(region, location)
                                 select new Action(() =>
                                 {
                                     var resizeRegion = region;
                                     Array.Resize(ref resizeRegion, resizeRegion.Length + 1);
                                     for (int i = resizeRegion.Length - 1; i > nearestPoint; i--)
                                     {
                                         resizeRegion[i] = resizeRegion[i - 1];
                                     }
                                     resizeRegion[nearestPoint] = location;
                                     regions[selectedRoi.Value] = resizeRegion;
                                 });

            var pointDeletion = from clickEvt in mouseDoubleClick
                                where clickEvt.Button == MouseButtons.Right && selectedRoi.HasValue
                                let region = regions[selectedRoi.Value]
                                where region.Length > 3
                                let location = NormalizedLocation(clickEvt.X, clickEvt.Y)
                                let nearestPoint = NearestPoint(region, location)
                                select new Action(() =>
                                {
                                    var resizeRegion = new CvPoint[region.Length - 1];
                                    Array.Copy(region, resizeRegion, nearestPoint);
                                    Array.Copy(region, nearestPoint + 1, resizeRegion, nearestPoint, region.Length - nearestPoint - 1);
                                    regions[selectedRoi.Value] = resizeRegion;
                                });


            roiSelected.Subscribe(selection => selectedRoi = selection);
            pointMove.Subscribe(action => action());
            roiMove.Subscribe(action => action());
            pointInsertion.Subscribe(action => action());
            pointDeletion.Subscribe(action => action());
            regionInsertion.Subscribe(region =>
            {
                if (selectedRoi.HasValue) regions[selectedRoi.Value] = region;
                else
                {
                    selectedRoi = regions.Count;
                    regions.Add(region);
                }
            });
        }

        void PictureBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && selectedRoi.HasValue)
            {
                regions.RemoveAt(selectedRoi.Value);
                selectedRoi = null;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Tab && regions.Count > 0)
            {
                selectedRoi = ((selectedRoi ?? 0) + 1) % regions.Count;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        double TestIntersection(CvPoint[] region, CvPoint point)
        {
            var regionHandle = GCHandle.Alloc(region, GCHandleType.Pinned);
            try
            {
                using (var mat = new CvMat(region.Length, 1, CvMatDepth.CV_32S, 2, regionHandle.AddrOfPinnedObject()))
                {
                    return ImgProc.cvPointPolygonTest(mat, new CvPoint2D32f(point.X, point.Y), 1);
                }
            }
            finally { regionHandle.Free(); }
        }

        CvRect ClipRectangle(CvRect rect)
        {
            var clipX = rect.X < 0 ? -rect.X : 0;
            var clipY = rect.Y < 0 ? -rect.Y : 0;
            clipX += Math.Max(0, rect.X + rect.Width - Image.Width);
            clipY += Math.Max(0, rect.Y + rect.Height - Image.Height);

            rect.X = Math.Max(0, rect.X);
            rect.Y = Math.Max(0, rect.Y);
            rect.Width = rect.Width - clipX;
            rect.Height = rect.Height - clipY;
            return rect;
        }

        CvPoint NormalizedLocation(int x, int y)
        {
            return new CvPoint(
                (int)(x * Image.Width / (float)Canvas.Width),
                (int)(y * Image.Height / (float)Canvas.Height));
        }

        CvRect NormalizedRectangle(CvRect rect)
        {
            return new CvRect(
                (int)(rect.X * Image.Width / (float)Canvas.Width),
                (int)(rect.Y * Image.Height / (float)Canvas.Height),
                (int)(rect.Width * Image.Width / (float)Canvas.Width),
                (int)(rect.Height * Image.Width / (float)Canvas.Width));
        }

        int NearestPoint(CvPoint[] region, CvPoint location)
        {
            return (from point in region.Select((p, i) => new { p, i })
                    let distanceX = location.X - point.p.X
                    let distanceY = location.Y - point.p.Y
                    orderby distanceX * distanceX + distanceY * distanceY ascending
                    select point.i)
                    .FirstOrDefault();
        }

        public Collection<CvPoint[]> Regions
        {
            get { return regions; }
        }

        public event EventHandler SelectedRegionChanged;

        protected virtual void OnSelectedRegionChanged(EventArgs e)
        {
            var handler = SelectedRegionChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        void RenderRegion(CvPoint[] region, BeginMode mode, Color color)
        {
            GL.Color3(color);
            GL.Begin(mode);
            for (int i = 0; i < region.Length; i++)
            {
                GL.Vertex2(DrawingHelper.NormalizePoint(region[i], Image.Size));
            }
            GL.End();
        }

        protected override void OnLoad(EventArgs e)
        {
            GL.LineWidth(LineWidth);
            GL.PointSize(PointSize);
            GL.Enable(EnableCap.PointSmooth);
            base.OnLoad(e);
        }

        protected override void OnRenderFrame(EventArgs e)
        {
            GL.Color3(Color.White);
            base.OnRenderFrame(e);

            GL.Disable(EnableCap.Texture2D);
            foreach (var region in regions.Where((region, i) => i != selectedRoi))
            {
                RenderRegion(region, BeginMode.LineLoop, Color.Red);
            }

            if (selectedRoi.HasValue)
            {
                var region = regions[selectedRoi.Value];
                RenderRegion(region, BeginMode.LineLoop, Color.LimeGreen);
                RenderRegion(region, BeginMode.Points, Color.Blue);
            }
        }
    }
}
