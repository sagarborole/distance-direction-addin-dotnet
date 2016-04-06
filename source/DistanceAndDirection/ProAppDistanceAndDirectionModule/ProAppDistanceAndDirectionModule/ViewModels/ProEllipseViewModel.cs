﻿// Copyright 2016 Esri 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using DistanceAndDirectionLibrary;
using DistanceAndDirectionLibrary.Helpers;
using System;

namespace ProAppDistanceAndDirectionModule.ViewModels
{
    public class ProEllipseViewModel : ProTabBaseViewModel
    {
        public ProEllipseViewModel()
        {
            ActivateToolCommand = new ArcGIS.Desktop.Framework.RelayCommand(async () =>
            {
                await FrameworkApplication.SetCurrentToolAsync("ProAppDistanceAndDirectionModule_SketchTool");
                Mediator.NotifyColleagues("SET_SKETCH_TOOL_TYPE", ArcGIS.Desktop.Mapping.SketchGeometryType.AngledEllipse);
            });

            //Mediator.Register("SKETCH_COMPLETE", OnSketchComplete);

            EllipseType = EllipseTypes.Semi;
        }

        private void OnSketchComplete(object obj)
        {
            AddGraphicToMap(obj as ArcGIS.Core.Geometry.Geometry);
        }

        public new ArcGIS.Desktop.Framework.RelayCommand ActivateToolCommand { get; set; }

        #region Properties

        public EllipseTypes EllipseType { get; set; }
        public MapPoint CenterPoint { get; set; }

        AzimuthTypes azimuthType = AzimuthTypes.Degrees;
        public AzimuthTypes AzimuthType
        {
            get { return azimuthType; }
            set
            {
                var before = azimuthType;
                azimuthType = value;
                UpdateAzimuthFromTo(before, value);
            }
        }

        private MapPoint point2 = null;
        public override MapPoint Point2
        {
            get
            {
                return point2;
            }
            set
            {
                point2 = value;
                RaisePropertyChanged(() => Point2);
            }
        }

        private bool HasPoint3 = false;
        private MapPoint point3 = null;
        public MapPoint Point3
        {
            get
            {
                return point3;
            }
            set
            {
                point3 = value;
                RaisePropertyChanged(() => Point3);
            }
        }

        private double minorAxisDistance = 0.0;
        public double MinorAxisDistance
        {
            get
            {
                return minorAxisDistance;
            }
            set
            {
                minorAxisDistance = value;

                UpdateFeedbackWithEllipse();

                RaisePropertyChanged(() => MinorAxisDistance);
                RaisePropertyChanged(() => MinorAxisDistanceString);                
            }
        }

        private string minorAxisDistanceString = string.Empty;
        public string MinorAxisDistanceString
        {
            get
            {
                return MinorAxisDistance.ToString("G");
            }
            set
            {
                if (string.Equals(minorAxisDistanceString, value))
                    return;

                minorAxisDistanceString = value;
                double d = 0.0;
                if (double.TryParse(minorAxisDistanceString, out d))
                {
                    MinorAxisDistance = d;
                    RaisePropertyChanged(() => MinorAxisDistance);

                    // update feedback
                    //Point3 = UpdateFeedback(Point1, minorAxisDistance);
                }
                else
                {
                    throw new ArgumentException(DistanceAndDirectionLibrary.Properties.Resources.AEInvalidInput);
                }
            }
        }

        private double majorAxisDistance = 0.0;
        public double MajorAxisDistance
        {
            get
            {
                return majorAxisDistance;
            }
            set
            {
                majorAxisDistance = value;

                //Point2 = UpdateFeedback(Point1, MajorAxisDistance);

                UpdateFeedbackWithEllipse();

                RaisePropertyChanged(() => MajorAxisDistance);
                RaisePropertyChanged(() => MajorAxisDistanceString);
            }
        }

        private string majorAxisDistanceString = string.Empty;
        public string MajorAxisDistanceString
        {
            get
            {
                if (EllipseType == EllipseTypes.Full)
                {
                    return (MajorAxisDistance * 2.0).ToString("G");
                }
                return MajorAxisDistance.ToString("G");
            }
            set
            {
                if (string.Equals(majorAxisDistanceString, value))
                    return;

                majorAxisDistanceString = value;
                double d = 0.0;
                if (double.TryParse(majorAxisDistanceString, out d))
                {                            
                    MajorAxisDistance = d;
                }
                else
                {
                    throw new ArgumentException(DistanceAndDirectionLibrary.Properties.Resources.AEInvalidInput);
                }
            }
        }

        double azimuth = 0.0;
        public double Azimuth
        {
            get 
            {
                //return azimuth * 0.0174533; 
                //return ((azimuth - 90) / 180.0) * Math.PI;
                return azimuth;
            }
            set
            {
                azimuth = value;
                RaisePropertyChanged(() => Azimuth);

                // update feedback
                //Point2 = UpdateFeedback(Point1, MajorAxisDistance);

                UpdateFeedbackWithEllipse();

                AzimuthString = azimuth.ToString("G");
                RaisePropertyChanged(() => AzimuthString);
            }
        }

        string azimuthString = string.Empty;
        public string AzimuthString
        {
            get { return azimuthString; }
            set
            {
                // lets avoid an infinite loop here
                if (string.Equals(azimuthString, value))
                    return;

                azimuthString = value;
                // update azimuth
                double d = 0.0;
                if (double.TryParse(azimuthString, out d))
                {
                    Azimuth = d;
                }
                else
                {
                    throw new ArgumentException(DistanceAndDirectionLibrary.Properties.Resources.AEInvalidInput);
                }
            }
        }

        #endregion

        #region Commands

        // when someone hits the enter key, create geodetic graphic
        internal override void OnEnterKeyCommand(object obj)
        {
            if (MajorAxisDistance == 0.0 || Point1 == null || 
                MinorAxisDistance == 0.0 || Azimuth == 0.0)
            {
                return;
            }
            if (Point3 == null)
            {
                //Point3 = UpdateFeedback(Point1, MinorAxisDistance);
            }
            base.OnEnterKeyCommand(obj);
        }

        #endregion Commands

        #region Overriden Functions

        internal override void CreateMapElement()
        {
            if (Point1 == null || Point2 == null || Point3 == null)
            {
                return;
            }
            DrawEllipse();
            Reset(false);
        }

        internal override void OnMouseMoveEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as MapPoint;

            if (point == null)
                return;

            if (!HasPoint1)
            {
                Point1 = point;
            }
            else if (HasPoint1 && !HasPoint2)
            {
                // get major distance from polyline
                MajorAxisDistance = GeometryEngine.GeodesicDistance(Point1, point);
                // update bearing
                var segment = QueuedTask.Run(() =>
                {
                    return LineBuilder.CreateLineSegment(Point1, point);
                }).Result;

                UpdateAzimuth(segment.Angle);
            }
            else if (HasPoint1 && HasPoint2 && !HasPoint3)
            {
                MinorAxisDistance = GeometryEngine.GeodesicDistance(Point1, point);
            }
        }

        private void UpdateFeedbackWithEllipse(bool HasMinorAxis = true)
        {
            if (!HasPoint1)
                return;
            
            var minorAxis = MinorAxisDistance;
            if (!HasMinorAxis || minorAxis == 0.0)
                minorAxis = MajorAxisDistance;

            if (minorAxis > MajorAxisDistance)
                minorAxis = MajorAxisDistance;
            try
            {
                var param = new GeometryEngine.GeodesicEllipseParameter();

                param.Center = new Coordinate(Point1);
                param.AxisDirection = GetAzimuthAsRadians(); 
                param.LinearUnit = LinearUnit.Meters;
                param.OutGeometryType = GeometryType.Polyline;
                param.SemiAxis1Length = MajorAxisDistance;
                param.SemiAxis2Length = minorAxis;
                param.VertexCount = VertexCount;

                var geom = GeometryEngine.GeodesicEllipse(param, MapView.Active.Map.SpatialReference);

                ClearTempGraphics();
                AddGraphicToMap(Point1, ColorFactory.Green, true, 5.0);
                AddGraphicToMap(geom, ColorFactory.Grey, true);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        internal override void ResetPoints()
        {
            HasPoint1 = HasPoint2 = HasPoint3 = false;
        }

        internal override void OnNewMapPointEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as MapPoint;

            if (point == null)
                return;

            if (!HasPoint1)
            {
                Point1 = point;
                HasPoint1 = true;
                Point1Formatted = string.Empty;
                AddGraphicToMap(Point1, ColorFactory.Green, true, 5.0);

            }
            else if (!HasPoint2)
            {
                Point2 = point;
                HasPoint2 = true;
            }
            else if (!HasPoint3)
            {
                if (MajorAxisDistance >= MinorAxisDistance)
                {
                    ResetFeedback();
                    Point3 = point;
                    HasPoint3 = true;
                }
            }

            if (HasPoint1 && HasPoint2 && HasPoint3)
            {
                CreateMapElement();
                ResetPoints();
            }
        }

        internal override void Reset(bool toolReset)
        {
            base.Reset(toolReset);
            HasPoint3 = false;
            Point3 = null;

            MajorAxisDistance = 0.0;
            MinorAxisDistance = 0.0;
            Azimuth = 0.0;
        }
        #endregion

        #region Private Functions

        private void UpdateAzimuthFromTo(AzimuthTypes fromType, AzimuthTypes toType)
        {
            try
            {
                double angle = Azimuth;

                if (fromType == AzimuthTypes.Degrees && toType == AzimuthTypes.Mils)
                    angle *= 17.777777778;
                else if (fromType == AzimuthTypes.Mils && toType == AzimuthTypes.Degrees)
                    angle *= 0.05625;

                Azimuth = angle;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private double GetAzimuthAsDegrees()
        {
            if (AzimuthType == AzimuthTypes.Mils)
            {
                return Azimuth * 0.05625;
            }

            return Azimuth;
        }

        private void UpdateAzimuth(double radians)
        {
            var degrees = radians * (180.0 / Math.PI);

            if (AzimuthType == AzimuthTypes.Degrees)
                Azimuth = degrees;
            else if (AzimuthType == AzimuthTypes.Mils)
                Azimuth = degrees * 17.777777778;
        }

        private double GetAzimuthAsRadians()
        {
            double result = GetAzimuthAsDegrees();

            return result * (Math.PI / 180.0);
        }

        private void DrawEllipse()
        {
            try
            {
                var param = new GeometryEngine.GeodesicEllipseParameter();

                param.Center = new Coordinate(Point1);
                param.AxisDirection = GetAzimuthAsRadians();
                param.LinearUnit = LinearUnit.Meters;
                param.OutGeometryType = GeometryType.Polygon;
                param.SemiAxis1Length = MajorAxisDistance;
                param.SemiAxis2Length = MinorAxisDistance;
                param.VertexCount = VertexCount;

                var geom = GeometryEngine.GeodesicEllipse(param, MapView.Active.Map.SpatialReference);

                AddGraphicToMap(geom, new CIMRGBColor() { R = 255, B = 0, G = 0, Alpha = 25 });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion

    }
}
