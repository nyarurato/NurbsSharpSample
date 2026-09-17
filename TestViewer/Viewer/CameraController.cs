using OpenTK.Mathematics;

namespace NurbsSharp.Samples.Viewer
{
    public class CameraController
    {
        public Matrix4 Model { get; private set; } = Matrix4.Identity;
        public Matrix4 View { get; private set; } = Matrix4.Identity;
        public Matrix4 Projection { get; private set; } = Matrix4.Identity;

        private Vector3 _surfaceCenter = new Vector3(2.0f, 2.0f, 0.0f);
        private float _rotationAngle = 0.0f;
        private float _rotationSpeed = 0.5f;
        private bool _rotationEnabled = true;

        public bool RotationEnabled
        {
            get => _rotationEnabled;
            set => _rotationEnabled = value;
        }

        public void Initialize(int width, int height)
        {
            _rotationAngle = 0.0f;
            _rotationEnabled = true;
            Model = Matrix4.Identity;
            UpdateView();
            Resize(width, height);
        }

        public void SetCenter(NurbsSharp.Core.Vector3Double center)
        {
            _surfaceCenter = new Vector3((float)center.X, (float)center.Y, (float)center.Z);
            UpdateView();
        }

        public void Update(float deltaTime)
        {
            if (_rotationEnabled)
                _rotationAngle += deltaTime * _rotationSpeed;

            Model = Matrix4.CreateTranslation(-_surfaceCenter) *
                Matrix4.CreateRotationZ(_rotationAngle) *
                Matrix4.CreateTranslation(_surfaceCenter);
        }

        public Matrix4 GetMVP()
        {
            return Model * View * Projection;
        }

        public void Resize(int width, int height)
        {
            int safeHeight = Math.Max(height, 1);
            Projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                (float)width / safeHeight,
                0.1f,
                100.0f
            );
        }

        private void UpdateView()
        {
            View = Matrix4.LookAt(
                _surfaceCenter + new Vector3(4.0f, 4.0f, 10.0f),
                _surfaceCenter,
                Vector3.UnitZ
            );
        }
    }
}
