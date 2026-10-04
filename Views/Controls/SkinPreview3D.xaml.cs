using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace VibrantbitLauncher.Views.Controls
{
    public partial class SkinPreview3D : UserControl
    {
        public static readonly DependencyProperty SkinSourceProperty =
            DependencyProperty.Register(
                nameof(SkinSource),
                typeof(string),
                typeof(SkinPreview3D),
                new PropertyMetadata(null, OnSkinSourceChanged));

        public string SkinSource
        {
            get => (string)GetValue(SkinSourceProperty);
            set => SetValue(SkinSourceProperty, value);
        }

        private readonly DispatcherTimer _autoRotateTimer;
        private double _rotationY = 0;
        private double _rotationX = 10;
        private bool _isDragging = false;
        private Point _lastMousePos;
        private const double CameraDistance = 16;
        private const double CameraTargetY = 3;

        public SkinPreview3D()
        {
            InitializeComponent();
            _autoRotateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            _autoRotateTimer.Tick += (s, e) =>
            {
                if (!_isDragging)
                {
                    _rotationY += 0.4;
                    UpdateCamera();
                }
            };
            _autoRotateTimer.Start();

            viewport.MouseLeftButtonDown += Viewport_MouseLeftButtonDown;
            viewport.MouseLeftButtonUp += Viewport_MouseLeftButtonUp;
            viewport.MouseMove += Viewport_MouseMove;
            viewport.MouseLeave += (s, e) => _isDragging = false;

            Loaded += (s, e) => UpdateCamera();
        }

        private static void OnSkinSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkinPreview3D ctrl)
                ctrl.RebuildModel();
        }

        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePos = e.GetPosition(viewport);
            viewport.CaptureMouse();
        }

        private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            viewport.ReleaseMouseCapture();
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            var pos = e.GetPosition(viewport);
            var dx = pos.X - _lastMousePos.X;
            var dy = pos.Y - _lastMousePos.Y;
            _rotationY += dx * 0.5;
            _rotationX -= dy * 0.5;
            _rotationX = Math.Max(-60, Math.Min(60, _rotationX));
            _lastMousePos = pos;
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            double az = _rotationY * Math.PI / 180;
            double el = _rotationX * Math.PI / 180;
            double x = CameraDistance * Math.Sin(az) * Math.Cos(el);
            double y = CameraTargetY + CameraDistance * Math.Sin(el);
            double z = CameraDistance * Math.Cos(az) * Math.Cos(el);
            camera.Position = new Point3D(x, y, z);
            camera.LookDirection = new Vector3D(-x, CameraTargetY - y, -z);
        }

        private void RebuildModel()
        {
            // 移除旧的角色模型（保留灯光）
            for (int i = modelGroup.Children.Count - 1; i >= 0; i--)
            {
                if (modelGroup.Children[i] is GeometryModel3D)
                    modelGroup.Children.RemoveAt(i);
            }

            if (string.IsNullOrEmpty(SkinSource) || !File.Exists(SkinSource))
            {
                // 使用默认史蒂夫皮肤颜色
                AddDefaultCharacter();
                return;
            }

            try
            {
                var skinBitmap = new BitmapImage();
                skinBitmap.BeginInit();
                skinBitmap.UriSource = new Uri(SkinSource, UriKind.Absolute);
                skinBitmap.CacheOption = BitmapCacheOption.OnLoad;
                skinBitmap.EndInit();
                skinBitmap.Freeze();

                var skinBrush = new ImageBrush(skinBitmap)
                {
                    ViewportUnits = BrushMappingMode.Absolute,
                    TileMode = TileMode.None
                };

                AddCharacter(new DiffuseMaterial(skinBrush));
            }
            catch
            {
                AddDefaultCharacter();
            }
        }

        private void AddDefaultCharacter()
        {
            // 史蒂夫默认色
            var skin = new SolidColorBrush(Color.FromRgb(0x62, 0x8B, 0x41));
            AddCharacter(new DiffuseMaterial(skin));
        }

        private void AddCharacter(Material material)
        {
            // 身体各部位尺寸（Minecraft 单位）
            // 腿: 4x12x4, 身体: 8x12x4, 手臂: 4x12x4, 头: 8x8x8
            // Y 轴向上，脚在 y=-12

            // 右腿
            modelGroup.Children.Add(CreateBox(
                -2, -6, 0, 4, 12, 4, material,
                new FaceUV(4, 16, 8, 20),   // top
                new FaceUV(8, 16, 12, 20),   // bottom
                new FaceUV(0, 20, 4, 32),    // right (outer)
                new FaceUV(4, 20, 8, 32),    // front
                new FaceUV(8, 20, 12, 32),   // left (inner)
                new FaceUV(12, 20, 16, 32))); // back

            // 左腿
            modelGroup.Children.Add(CreateBox(
                2, -6, 0, 4, 12, 4, material,
                new FaceUV(20, 48, 24, 52),
                new FaceUV(24, 48, 28, 52),
                new FaceUV(16, 52, 20, 64),
                new FaceUV(20, 52, 24, 64),
                new FaceUV(24, 52, 28, 64),
                new FaceUV(28, 52, 32, 64)));

            // 身体
            modelGroup.Children.Add(CreateBox(
                0, 6, 0, 8, 12, 4, material,
                new FaceUV(20, 16, 28, 20),
                new FaceUV(28, 16, 36, 20),
                new FaceUV(16, 20, 20, 32),
                new FaceUV(20, 20, 28, 32),
                new FaceUV(28, 20, 32, 32),
                new FaceUV(32, 20, 40, 32)));

            // 右臂
            modelGroup.Children.Add(CreateBox(
                -6, 6, 0, 4, 12, 4, material,
                new FaceUV(44, 16, 48, 20),
                new FaceUV(48, 16, 52, 20),
                new FaceUV(40, 20, 44, 32),
                new FaceUV(44, 20, 48, 32),
                new FaceUV(48, 20, 52, 32),
                new FaceUV(52, 20, 56, 32)));

            // 左臂
            modelGroup.Children.Add(CreateBox(
                6, 6, 0, 4, 12, 4, material,
                new FaceUV(36, 48, 40, 52),
                new FaceUV(40, 48, 44, 52),
                new FaceUV(32, 52, 36, 64),
                new FaceUV(36, 52, 40, 64),
                new FaceUV(40, 52, 44, 64),
                new FaceUV(44, 52, 48, 64)));

            // 头
            modelGroup.Children.Add(CreateBox(
                0, 16, 0, 8, 8, 8, material,
                new FaceUV(8, 0, 16, 8),
                new FaceUV(16, 0, 24, 8),
                new FaceUV(0, 8, 8, 16),
                new FaceUV(8, 8, 16, 16),
                new FaceUV(16, 8, 24, 16),
                new FaceUV(24, 8, 32, 16)));
        }

        /// <summary>
        /// 表示皮肤纹理上的一个矩形区域（Minecraft 皮肤坐标，左上角为原点，单位像素）
        /// </summary>
        private readonly struct FaceUV
        {
            public readonly double U0, V0, U1, V1;
            public FaceUV(double u0, double v0, double u1, double v1)
            {
                U0 = u0; V0 = v0; U1 = u1; V1 = v1;
            }
        }

        /// <summary>
        /// 创建一个带皮肤纹理的立方体。
        /// 坐标：X 向右，Y 向上，Z 向前（朝向观察者）。
        /// </summary>
        private static GeometryModel3D CreateBox(
            double cx, double cy, double cz,
            double w, double h, double d,
            Material material,
            FaceUV top, FaceUV bottom,
            FaceUV right, FaceUV front, FaceUV left, FaceUV back)
        {
            double hw = w / 2, hh = h / 2, hd = d / 2;
            var positions = new Point3DCollection();
            var normals = new Vector3DCollection();
            var texCoords = new PointCollection();
            var indices = new Int32Collection();

            // 每个面 4 个顶点，6 个面 = 24 顶点
            // 顺序: 0-1-2, 0-2-3 (逆时针为正面)

            // Front (z = +hd, normal +Z)
            AddFace(positions, normals, texCoords, indices,
                new Point3D(cx - hw, cy - hh, cz + hd),
                new Point3D(cx + hw, cy - hh, cz + hd),
                new Point3D(cx + hw, cy + hh, cz + hd),
                new Point3D(cx - hw, cy + hh, cz + hd),
                new Vector3D(0, 0, 1),
                front);

            // Back (z = -hd, normal -Z)
            AddFace(positions, normals, texCoords, indices,
                new Point3D(cx + hw, cy - hh, cz - hd),
                new Point3D(cx - hw, cy - hh, cz - hd),
                new Point3D(cx - hw, cy + hh, cz - hd),
                new Point3D(cx + hw, cy + hh, cz - hd),
                new Vector3D(0, 0, -1),
                back);

            // Right (x = +hw, normal +X)
            AddFace(positions, normals, texCoords, indices,
                new Point3D(cx + hw, cy - hh, cz + hd),
                new Point3D(cx + hw, cy - hh, cz - hd),
                new Point3D(cx + hw, cy + hh, cz - hd),
                new Point3D(cx + hw, cy + hh, cz + hd),
                new Vector3D(1, 0, 0),
                right);

            // Left (x = -hw, normal -X)
            AddFace(positions, normals, texCoords, indices,
                new Point3D(cx - hw, cy - hh, cz - hd),
                new Point3D(cx - hw, cy - hh, cz + hd),
                new Point3D(cx - hw, cy + hh, cz + hd),
                new Point3D(cx - hw, cy + hh, cz - hd),
                new Vector3D(-1, 0, 0),
                left);

            // Top (y = +hh, normal +Y)
            AddFace(positions, normals, texCoords, indices,
                new Point3D(cx - hw, cy + hh, cz + hd),
                new Point3D(cx + hw, cy + hh, cz + hd),
                new Point3D(cx + hw, cy + hh, cz - hd),
                new Point3D(cx - hw, cy + hh, cz - hd),
                new Vector3D(0, 1, 0),
                top);

            // Bottom (y = -hh, normal -Y)
            AddFace(positions, normals, texCoords, indices,
                new Point3D(cx - hw, cy - hh, cz - hd),
                new Point3D(cx + hw, cy - hh, cz - hd),
                new Point3D(cx + hw, cy - hh, cz + hd),
                new Point3D(cx - hw, cy - hh, cz + hd),
                new Vector3D(0, -1, 0),
                bottom);

            var mesh = new MeshGeometry3D
            {
                Positions = positions,
                Normals = normals,
                TextureCoordinates = texCoords,
                TriangleIndices = indices
            };

            return new GeometryModel3D(mesh, material);
        }

        private static void AddFace(
            Point3DCollection positions, Vector3DCollection normals,
            PointCollection texCoords, Int32Collection indices,
            Point3D p0, Point3D p1, Point3D p2, Point3D p3,
            Vector3D normal, FaceUV uv)
        {
            int baseIdx = positions.Count;
            // Minecraft 皮肤坐标左上角为(0,0)，WPF 纹理坐标左下角为(0,0)，需要翻转 V
            // 皮肤是 64x64
            double u0 = uv.U0 / 64.0, u1 = uv.U1 / 64.0;
            double v0 = 1.0 - uv.V0 / 64.0, v1 = 1.0 - uv.V1 / 64.0;

            // p0=左下, p1=右下, p2=右上, p3=左上
            positions.Add(p0); texCoords.Add(new Point(u0, v1));
            positions.Add(p1); texCoords.Add(new Point(u1, v1));
            positions.Add(p2); texCoords.Add(new Point(u1, v0));
            positions.Add(p3); texCoords.Add(new Point(u0, v0));

            for (int i = 0; i < 4; i++) normals.Add(normal);

            indices.Add(baseIdx);
            indices.Add(baseIdx + 1);
            indices.Add(baseIdx + 2);
            indices.Add(baseIdx);
            indices.Add(baseIdx + 2);
            indices.Add(baseIdx + 3);
        }
    }
}
