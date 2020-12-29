using System;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;
using UnityEngine.UI;

namespace K4A.VFX
{
    public class AzureKinectSensor : MonoBehaviour
    {
        private Device kinect;

        private void Start()
        {
            kinect = Device.Open();
            kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });
            
        }

        private void OnApplicationQuit()
        {
            if (kinect != null)
            {
                kinect.StopCameras();
                kinect.Dispose();
            }
        }
    }
}