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

// System
using System;

// Esri
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Controls;

using DistanceAndDirectionLibrary.Helpers;
using DistanceAndDirectionLibrary;
using System.Windows.Forms;

namespace ArcMapAddinDistanceAndDirection
{
    public class MapPointTool : ESRI.ArcGIS.Desktop.AddIns.Tool
    {
        ISnappingEnvironment m_SnappingEnv;
        IPointSnapper m_Snapper;
        ISnappingFeedback m_SnappingFeedback;

        public MapPointTool()
        {
        }

        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }

        protected override void OnActivate()
        {
            //Get the snap environment and initialize the feedback
            UID snapUID = new UID();
            this.Cursor = Cursors.Cross;
            snapUID.Value = "{E07B4C52-C894-4558-B8D4-D4050018D1DA}";
            m_SnappingEnv = ArcMap.Application.FindExtensionByCLSID(snapUID) as ISnappingEnvironment;

            if (m_SnappingEnv !=  null)
                m_Snapper = m_SnappingEnv.PointSnapper;

            m_SnappingFeedback = new SnappingFeedbackClass();
            m_SnappingFeedback.Initialize(ArcMap.Application, m_SnappingEnv, true);
        }

        protected override void OnMouseDown(ESRI.ArcGIS.Desktop.AddIns.Tool.MouseEventArgs arg)
        {
            if (arg.Button != System.Windows.Forms.MouseButtons.Left)
                return;

            try
            {
                //Get the active view from the ArcMap static class.
                IActiveView activeView = (IActiveView)ArcMap.Document.FocusMap;

                var point = (IPoint)activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(arg.X, arg.Y);
                ISnappingResult snapResult = null;
                //Try to snap the current position
                snapResult = m_Snapper.Snap(point);
                m_SnappingFeedback.Update(null, 0);
                if (snapResult != null && snapResult.Location != null)
                    point = snapResult.Location;

                Mediator.NotifyColleagues(Constants.NEW_MAP_POINT, point);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }
        protected override void OnMouseMove(MouseEventArgs arg)
        {
            try
            {
                IActiveView activeView = ArcMap.Document.FocusMap as IActiveView;

                var point = activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(arg.X, arg.Y) as IPoint;
                ISnappingResult snapResult = null;
                //Try to snap the current position
                snapResult = m_Snapper.Snap(point);
                m_SnappingFeedback.Update(snapResult, 0);
                if (snapResult != null && snapResult.Location != null)
                    point = snapResult.Location;

                Mediator.NotifyColleagues(Constants.MOUSE_MOVE_POINT, point);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        protected override void OnDoubleClick()
        {
            Mediator.NotifyColleagues(Constants.MOUSE_DOUBLE_CLICK, null);
        }

        protected override bool OnDeactivate()
        {
            return true;   
        }

        // If the user presses Escape cancel the sketch
        protected override void OnKeyDown(KeyEventArgs k)
        {
            if (k.KeyCode == Keys.Escape)
            {
                Mediator.NotifyColleagues(DistanceAndDirectionLibrary.Constants.KEYPRESS_ESCAPE, null);
            }
        }

    }

}
