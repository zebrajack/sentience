/*
    Stereo camera calibration class, used for automatic calibration of two cameras
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using sentience.core;

namespace sentience.calibration
{
    public class calibrationStereo
    {
        public calibration leftcam, rightcam;

        // name of the WDM camera driver
        public String DriverName = "";
        
        // position and orientation of this camera relative to the head
        public pos3D positionOrientation = new pos3D(0, 0, 0);

        // horizontal and vertical offset of the right image relative to the left image
        public int offset_x = 0, offset_y = 0;

        // focal length in millimetres
        public float focalLength = 5;

        // camera baseline distance in millimetres
        public float baseline = 100.0f;

        /// <summary>
        /// set the position of the centre spot relative to the centre of the calibration pattern
        /// </summary>
        /// <param name="position"></param>
        public void setCentreSpotPosition(int position)
        {
            leftcam.centre_spot_position = position;
            rightcam.centre_spot_position = position;
        }

        public void update()
        {
            if ((leftcam.centre_spot_rectified != null) && (rightcam.centre_spot_rectified != null))
            {
                offset_y = (int)(rightcam.centre_spot_rectified.y - leftcam.centre_spot_rectified.y);
            }
        }

        public void updateCalibrationMaps()
        {
            leftcam.updateCalibrationMap();
            rightcam.updateCalibrationMap();
        }

        /// <summary>
        /// constructor
        /// </summary>
        public calibrationStereo()
        {
            leftcam = new calibration();
            rightcam = new calibration();
        }

        #region "saving and loading"

        public XmlElement getXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement nodeStereoCamera = doc.CreateElement("StereoCamera");
            parent.AppendChild(nodeStereoCamera);

            util.AddComment(doc, nodeStereoCamera, "Name of the WDM software driver for the cameras");
            util.AddTextElement(doc, nodeStereoCamera, "DriverName", DriverName);

            util.AddComment(doc, nodeStereoCamera, "Position and orientation of the stereo camera relative to the robots head or body");
            nodeStereoCamera.AppendChild(positionOrientation.getXml(doc));

            util.AddComment(doc, nodeStereoCamera, "Focal length in millimetres");
            util.AddTextElement(doc, nodeStereoCamera, "FocalLengthMillimetres", Convert.ToString(focalLength));

            util.AddComment(doc, nodeStereoCamera, "Camera baseline distance in millimetres");
            util.AddTextElement(doc, nodeStereoCamera, "BaselineMillimetres", Convert.ToString(baseline));

            util.AddComment(doc, nodeStereoCamera, "Calibration Data");

            XmlElement nodeCalibration = doc.CreateElement("Calibration");
            nodeStereoCamera.AppendChild(nodeCalibration);

            String offsets = Convert.ToString(offset_x) + "," +
                             Convert.ToString(offset_y);
            util.AddComment(doc, nodeCalibration, "Image offsets in pixels");
            util.AddTextElement(doc, nodeCalibration, "Offsets", offsets);

            XmlElement elem = leftcam.getXml(doc);
            nodeCalibration.AppendChild(elem);
            elem = rightcam.getXml(doc);
            nodeCalibration.AppendChild(elem);

            return (nodeStereoCamera);
        }

        /// <summary>
        /// return an Xml document containing camera calibration parameters
        /// </summary>
        /// <returns></returns>
        private XmlDocument getXmlDocument()
        {
            // Create the document.
            XmlDocument doc = new XmlDocument();

            // Insert the xml processing instruction and the root node
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "ISO-8859-1", null);
            doc.PrependChild(dec);

            XmlNode commentnode = doc.CreateComment("Sentience 3D Perception System");
            doc.AppendChild(commentnode);

            XmlElement nodeSentience = doc.CreateElement("Sentience");
            doc.AppendChild(nodeSentience);

            nodeSentience.AppendChild(getXml(doc, nodeSentience));

            return (doc);
        }

        /// <summary>
        /// save camera calibration parameters as an xml file
        /// </summary>
        /// <param name="filename">file name to save as</param>
        public void Save(String filename)
        {
            XmlDocument doc = getXmlDocument();
            doc.Save(filename);
        }

        /// <summary>
        /// load camera calibration parameters from file
        /// </summary>
        /// <param name="filename"></param>
        public void Load(String filename)
        {
            if (File.Exists(filename))
            {
                // use an XmlTextReader to open an XML document
                XmlTextReader xtr = new XmlTextReader(filename);
                xtr.WhitespaceHandling = WhitespaceHandling.None;

                // load the file into an XmlDocuent
                XmlDocument xd = new XmlDocument();
                xd.Load(xtr);

                // get the document root node
                XmlNode xnodDE = xd.DocumentElement;

                // recursively walk the node tree
                int cameraIndex = 0;
                LoadFromXml(xnodDE, 0, ref cameraIndex);

                // close the reader
                xtr.Close();
            }
        }

        /// <summary>
        /// parse an xml node to extract camera calibration parameters
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        public void LoadFromXml(XmlNode xnod, int level, ref int cameraIndex)
        {
            XmlNode xnodWorking;

            if (xnod.Name == "Camera")
            {
                if (cameraIndex == 0)
                    leftcam.LoadFromXml(xnod, level);
                else
                    rightcam.LoadFromXml(xnod, level);
                cameraIndex++;
            }
            else
            {
                if (xnod.Name == "DriverName")
                {
                    DriverName = xnod.InnerText;
                }

                if (xnod.Name == "PositionOrientation")
                {
                    positionOrientation.LoadFromXml(xnod, level);
                }

                if (xnod.Name == "Offsets")
                {
                    String[] offsets = xnod.InnerText.Split(',');
                    offset_x = Convert.ToInt32(offsets[0]);
                    offset_y = Convert.ToInt32(offsets[1]);
                }

                if (xnod.Name == "FocalLengthMillimetres")
                {
                    focalLength = Convert.ToSingle(xnod.InnerText);
                }

                if (xnod.Name == "BaselineMillimetres")
                {
                    baseline = Convert.ToSingle(xnod.InnerText);
                }                

                // call recursively on all children of the current node
                if (xnod.HasChildNodes)
                {
                    xnodWorking = xnod.FirstChild;
                    while (xnodWorking != null)
                    {
                        LoadFromXml(xnodWorking, level + 1, ref cameraIndex);
                        xnodWorking = xnodWorking.NextSibling;
                    }
                }
            }
        }

        #endregion

    }
}
