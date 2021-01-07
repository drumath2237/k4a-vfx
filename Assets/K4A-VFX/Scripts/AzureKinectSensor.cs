using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;
using UnityEngine.VFX;

namespace K4A.VFX
{
    public class AzureKinectSensor : MonoBehaviour
    {
        private Device kinect;

        private bool isRunning = false;

        private byte[] _rawColorData = null;
        private Color[] xyzs = null;

        private static readonly int MainTex = Shader.PropertyToID("_BaseColorMap");

        private CameraCalibration _depthCameraCalibration;
        private Transformation _kinectTransformation;

        private Texture2D _colorTexture2D;
        private Texture2D _depthTexture2D;

        [SerializeField] private VisualEffect _effect;

        private readonly int propertyWidth = Shader.PropertyToID("width");
        private readonly int propertyHeight = Shader.PropertyToID("height");
        private readonly int propertyColorImage = Shader.PropertyToID("colorImage");
        private readonly int propertyXyzImage = Shader.PropertyToID("xyzImage");

        [SerializeField] private GameObject _previewPlane;

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
            _depthCameraCalibration = kinect.GetCalibration().DepthCameraCalibration;
            _kinectTransformation = kinect.GetCalibration().CreateTransformation();

            // texture settings
            _colorTexture2D = new Texture2D(_depthCameraCalibration.ResolutionWidth,
                _depthCameraCalibration.ResolutionHeight, TextureFormat.BGRA32, false);
            
            _depthTexture2D = new Texture2D(_depthCameraCalibration.ResolutionWidth,
                _depthCameraCalibration.ResolutionHeight, TextureFormat.RGBAFloat, false)
            {
                filterMode = FilterMode.Point
            };
            
            if (_previewPlane != null)
            {
                _previewPlane.GetComponent<MeshRenderer>().material.SetTexture(MainTex, _colorTexture2D);
                _previewPlane.transform.localScale = new Vector3(1f, 1f,
                    (float) _depthCameraCalibration.ResolutionHeight / _depthCameraCalibration.ResolutionWidth);
            }



            // vfx settings
            if (_effect != null)
            {
                _effect.SetUInt(propertyWidth, (uint) _depthCameraCalibration.ResolutionWidth);
                _effect.SetUInt(propertyHeight, (uint) _depthCameraCalibration.ResolutionHeight);
                _effect.SetTexture(propertyColorImage, _colorTexture2D);
                _effect.SetTexture(propertyXyzImage, _depthTexture2D);
            }

            UniTask.Run(() =>
            {
                while (isRunning)
                {
                    using (var capture = kinect.GetCapture())
                    {
                        Image colorImage = _kinectTransformation.ColorImageToDepthCamera(capture);

                        _rawColorData = colorImage.Memory.ToArray();

                        Image xyzImage = _kinectTransformation.DepthImageToPointCloud(capture.Depth);
                        xyzs = xyzImage.GetPixels<Short3>().ToArray()
                            .Select(short3 => new Color(short3.X / 100.0f, -short3.Y / 100.0f, short3.Z / 100.0f))
                            .ToArray();
                    }
                }
            }, true, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void Update()
        {
            if (_rawColorData != null)
            {
                _colorTexture2D.LoadRawTextureData(_rawColorData);
                _colorTexture2D.Apply();
            }

            if (xyzs != null)
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