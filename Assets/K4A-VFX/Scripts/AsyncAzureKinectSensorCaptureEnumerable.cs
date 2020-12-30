using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Microsoft.Azure.Kinect.Sensor;

namespace K4A.VFX
{
    public class AsyncAzureKinectSensorCaptureEnumerable : IUniTaskAsyncEnumerable<Capture>
    {
        private Device _kinect;

        private AsyncAzureKinectSensorCaptureEnumrator enumerator;

        public AsyncAzureKinectSensorCaptureEnumerable(Device kinect, CancellationToken token)
        {
            this._kinect = kinect;
            enumerator = new AsyncAzureKinectSensorCaptureEnumrator(this._kinect, token);
        }

        public IUniTaskAsyncEnumerator<Capture> GetAsyncEnumerator(
            CancellationToken cancellationToken = new CancellationToken())
        {
            return enumerator;
        }
    }
}