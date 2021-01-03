using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;

namespace K4A.VFX
{
    public class AzureKinectSensor : MonoBehaviour
    {
        private Device kinect;

        private bool isRunning = false;

        private Material _material;

        private Color[] _colors = default;

        private void Start()
        {
            _material = GetComponent<Renderer>().material;

            kinect = Device.Open();
            kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });

            isRunning = true;

            UniTaskAsyncEnumerable.Create<BGRA[]>(async (writer, token) =>
                {
                    while (isRunning)
                    {
                        using (Capture capture = await UniTask.Run(() => kinect.GetCapture(), cancellationToken: token))
                        {
                            writer.YieldAsync(capture.Color.GetPixels<BGRA>().ToArray());
                        }
                    }
                }).Select(arr =>
                {
                    return arr.Select(bgra =>
                            new Color(bgra.R / 255.0f, bgra.G / 255.0f, bgra.B / 255.0f))
                        .ToArray();
                })
                .ForEachAsync((colors, token) =>
                {
                    Debug.Log(colors[0].ToString());
                    _colors = colors;
                }, this.GetCancellationTokenOnDestroy());

            StartCoroutine(TextureLoop());
        }

        IEnumerator TextureLoop()
        {
            while (true)
            {
                if (_colors != null)
                {
                    var cameraCalibration = kinect.GetCalibration().ColorCameraCalibration;
                    var width = cameraCalibration.ResolutionWidth;
                    var height = cameraCalibration.ResolutionHeight;

                    if (_colors.Length == width * height)
                    {
                        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                        texture.SetPixels(0, 0, width, height, _colors);
                        texture.Apply();
                        Destroy(_material.mainTexture);
                        _material.mainTexture = texture;
                    }
                }

                yield return null;
                yield return null;
            }
        }

        private void OnApplicationQuit()
        {
            isRunning = false;
            if (kinect != null)
            {
                kinect.StopCameras();
                kinect.Dispose();
            }
        }
    }
}