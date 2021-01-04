using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;
using UnityEditor.MemoryProfiler;
using UnityEngine.VFX;
using Color = UnityEngine.Color;

namespace K4A.VFX
{
    public class AzureKinectSensor : MonoBehaviour
    {
        private Device kinect;

        private bool isRunning = false;

        private byte[] _rawColorData = null;
        private byte[] _rawDepthData = null;

        private static readonly int MainTex = Shader.PropertyToID("_BaseColorMap");

        private Color32[] _colors = null;
        private Color[] xyzs = null;

        private CameraCalibration _colorCameraCalibration;
        private CameraCalibration _depthCameraCalibration;
        private Transformation _kinectTransformation;

        private Texture2D _colorTexture2D;
        private Texture2D _depthTexture2D;

        [SerializeField] private VisualEffect _effect;

        private readonly int propertyWidth = Shader.PropertyToID("width");
        private readonly int propertyHeight = Shader.PropertyToID("height");
        private readonly int propertyColorImage = Shader.PropertyToID("colorImage");
        private readonly int propertyXyzImage = Shader.PropertyToID("xyzImage");
        

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

            isRunning = true;

            // get calibrations
            _colorCameraCalibration = kinect.GetCalibration().ColorCameraCalibration;
            _depthCameraCalibration = kinect.GetCalibration().DepthCameraCalibration;
            _kinectTransformation = kinect.GetCalibration().CreateTransformation();
            
            // texture settings
            _colorTexture2D = new Texture2D(_depthCameraCalibration.ResolutionWidth,
                _depthCameraCalibration.ResolutionHeight, TextureFormat.BGRA32, false);
            // GetComponent<MeshRenderer>().material.SetTexture(MainTex, _colorTexture2D);
            
            _depthTexture2D = new Texture2D(_depthCameraCalibration.ResolutionWidth,
                _depthCameraCalibration.ResolutionHeight, TextureFormat.RGBAFloat, false);
            // _depthTexture2D.wrapMode = TextureWrapMode.Clamp;
            _depthTexture2D.filterMode = FilterMode.Point;
            GetComponent<MeshRenderer>().material.SetTexture(MainTex, _depthTexture2D);


            // plane scaling
            transform.localScale = new Vector3(1f, 1f,
                (float) _depthCameraCalibration.ResolutionHeight / _depthCameraCalibration.ResolutionWidth);
            
            
            // vfx settings
            if (_effect != null)
            {
                _effect.SetUInt(propertyWidth, (uint)_depthCameraCalibration.ResolutionWidth);
                _effect.SetUInt(propertyHeight, (uint) _depthCameraCalibration.ResolutionHeight);
                _effect.SetTexture(propertyColorImage, _colorTexture2D);
                _effect.SetTexture(propertyXyzImage, _depthTexture2D);
            }

            // UniTaskAsyncEnumerable.Create<BGRA[]>(async (writer, token) =>
            //     {
            //         await UniTask.Run(() =>
            //         {
            //             while (isRunning)
            //                 using (var capture = kinect.GetCapture())
            //                     writer.YieldAsync(capture.Color.GetPixels<BGRA>().ToArray());
            //         }, cancellationToken: token);
            //     })
            // .Select(arr =>
            // {
            //     return arr.Select(bgra =>
            //             new Color32(bgra.R, bgra.G, bgra.B, bgra.A))
            //         .ToArray();
            // })
            // .ForEachAsync(
            //     (colors, token) => { _colors = colors; }, this.GetCancellationTokenOnDestroy());
            //


            // UniTaskAsyncEnumerable.Create<byte[]>(async (writer, token) =>
            // {
            //     await UniTask.Run(() =>
            //     {
            //         while (isRunning)
            //         {
            //             using (var capture = kinect.GetCapture())
            //             {
            //                 writer.YieldAsync(capture.Color.Memory.ToArray());
            //             }
            //         }
            //     }, cancellationToken: token);
            // }).ForEachAsync(bytes => { _rawTexture = bytes; }, this.GetCancellationTokenOnDestroy());


            // KinectLoop();

            UniTask.Run(() =>
            {
                while (isRunning)
                {
                    using(var fakeDepth = new Image(ImageFormat.Depth16, _depthCameraCalibration.ResolutionWidth, _depthCameraCalibration.ResolutionHeight))
                    using (var capture = kinect.GetCapture())
                    {
                        Image colorImage = _kinectTransformation.ColorImageToDepthCamera(capture);

                        _rawColorData = colorImage.Memory.ToArray();
                        // _colors = colorImage.GetPixels<BGRA>().ToArray()
                        //     .Select(bgra => new Color32(bgra.R, bgra.G, bgra.B, bgra.A)).ToArray();

                        // _rawDepthData = capture.Depth.Memory.ToArray();

                        var depthValues = capture.Depth.GetPixels<ushort>().ToArray();
                        xyzs = depthValues.Select(arg => Mathf.Clamp01(arg/1000.0f)).Select(arg => new Color(arg, arg, arg)).ToArray();
                        
                        // Image xyzImage = _kinectTransformation.DepthImageToPointCloud(capture.Depth);
                        //
                        // xyzs = xyzImage.GetPixels<Short3>().ToArray()
                        // .Select(short3 => new Color(short3.X, short3.Y, short3.Z, 1.0f)).ToArray();
                    }
                }
            }, true, this.GetCancellationTokenOnDestroy()).Forget();
        }

        async void KinectLoop()
        {
            // while (isRunning)
            // {
            //     using (var capture = await Task.Run(() => kinect.GetCapture()))
            //     {
            //         // var bgraImage = capture.Color.GetPixels<BGRA>().ToArray();
            //         // _colors = bgraImage.Select(bgra => new Color32(bgra.R, bgra.G, bgra.B, bgra.A)).ToArray();
            //         _rawTexture = capture.Color.Memory.ToArray();
            //     }
            // }

            await Task.Run(() =>
            {
                while (isRunning)
                {
                    using (var capture = kinect.GetCapture())
                    {
                        // var bgraImage = capture.Color.GetPixels<BGRA>().ToArray();
                        // _colors = bgraImage.Select(bgra => new Color32(bgra.R, bgra.G, bgra.B, bgra.A)).ToArray();
                        try
                        {
                            _rawColorData = capture.Color.Memory.ToArray();
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);
                            throw;
                        }
                    }
                }
            });
        }

        private void Update()
        {
            if (_rawColorData != null)
            {
                _colorTexture2D.LoadRawTextureData(_rawColorData);
                _colorTexture2D.Apply();
            }
            else if (_colors != null)
            {
                _colorTexture2D.SetPixels32(_colors);
                _colorTexture2D.Apply();
            }

            if (_rawDepthData != null)
            {
                _depthTexture2D.LoadRawTextureData(_rawDepthData);
                _depthTexture2D.Apply();
            }else if (xyzs != null)
            {
                _depthTexture2D.SetPixels(xyzs);
                _depthTexture2D.Apply();
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