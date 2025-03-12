using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace ImageEditor
{
    public partial class MainWindow : Window
    {
        private BitmapImage? currentImage;
        private bool isActualSize = false;
        private string? currentImagePath;
        private bool isCropping = false;
        private System.Windows.Point cropStart;
        private bool isDragging = false;
        private Stack<BitmapImage> undoStack = new Stack<BitmapImage>();
        private const int MAX_UNDO_STEPS = 10;  // 최대 취소 단계 수

        public MainWindow()
        {
            InitializeComponent();
            SizeChanged += MainWindow_SizeChanged;

            // 단축키 이벤트 핸들러 연결
            btnOpen.Click += btnOpen_Click;
            btnReload.Click += btnReload_Click;
            btnSave.Click += btnSave_Click;
            btnFlipHorizontal.Click += btnFlipHorizontal_Click;
            btnRotateLeft.Click += btnRotateLeft_Click;
            btnRotateRight.Click += btnRotateRight_Click;
            btnCrop.Click += btnCrop_Click;
            btnResize.Click += btnResize_Click;
            btnAbout.Click += btnAbout_Click;
            btnUndo.Click += btnUndo_Click;

            // 키보드 이벤트 처리
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            // 마우스 이벤트 처리
            mainImage.MouseMove += MainImage_MouseMove;
            mainImage.MouseLeave += MainImage_MouseLeave;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateImageDisplay();
        }

        private void UpdateImageDisplay()
        {
            if (currentImage == null) return;

            if (isActualSize)
            {
                // 실제 크기로 표시
                mainImage.Width = currentImage.PixelWidth;
                mainImage.Height = currentImage.PixelHeight;
                mainImage.Stretch = Stretch.None;
                mainImage.MaxWidth = double.PositiveInfinity;
                mainImage.MaxHeight = double.PositiveInfinity;
                mainImage.HorizontalAlignment = HorizontalAlignment.Center;
                mainImage.VerticalAlignment = VerticalAlignment.Center;
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                // 창 크기에 맞게 자동 조정
                mainImage.Width = scrollViewer.ActualWidth;
                mainImage.Height = scrollViewer.ActualHeight;
                mainImage.Stretch = Stretch.Uniform;
                mainImage.HorizontalAlignment = HorizontalAlignment.Stretch;
                mainImage.VerticalAlignment = VerticalAlignment.Stretch;
                mainImage.MaxWidth = scrollViewer.ActualWidth;
                mainImage.MaxHeight = scrollViewer.ActualHeight;
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }

            txtImageSize.Text = $"이미지 크기: {currentImage.PixelWidth} x {currentImage.PixelHeight}";
        }

        private void chkActualSize_Click(object sender, RoutedEventArgs e)
        {
            isActualSize = chkActualSize.IsChecked ?? false;
            UpdateImageDisplay();
        }

        private void Image_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    LoadImage(files[0]);
                }
            }
        }

        private void Image_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void LoadImage(string filepath)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filepath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                undoStack.Clear();
                btnUndo.IsEnabled = false;
                currentImage = bitmap;
                currentImagePath = filepath;
                mainImage.Source = currentImage;
                UpdateImageDisplay();
                statusText.Text = "이미지가 로드되었습니다.";
                txtMousePos.Text = "위치: (-, -)";
                txtColorInfo.Text = "RGB: (-, -, -)";
                colorPreview.Fill = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지 로딩 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.ico;*.webp|모든 파일|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadImage(openFileDialog.FileName);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentImage == null) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG 이미지|*.png|JPEG 이미지|*.jpg|BMP 이미지|*.bmp|WebP 이미지|*.webp|아이콘 파일|*.ico"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string extension = Path.GetExtension(saveFileDialog.FileName).ToLower();
                    
                    if (extension == ".ico")
                    {
                        SaveAsIcon(saveFileDialog.FileName);
                    }
                    else if (extension == ".webp")
                    {
                        SaveAsWebP(saveFileDialog.FileName);
                    }
                    else
                    {
                        BitmapEncoder? encoder = null;

                        switch (extension)
                        {
                            case ".png":
                                encoder = new PngBitmapEncoder();
                                break;
                            case ".jpg":
                            case ".jpeg":
                                encoder = new JpegBitmapEncoder();
                                break;
                            case ".bmp":
                                encoder = new BmpBitmapEncoder();
                                break;
                        }

                        if (encoder != null)
                        {
                            encoder.Frames.Add(BitmapFrame.Create(currentImage));
                            using (var stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                            {
                                encoder.Save(stream);
                            }
                            statusText.Text = "이미지가 저장되었습니다.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"저장 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAsIcon(string filepath)
        {
            // 아이콘 크기 배열 (일반적으로 사용되는 크기들)
            int[] iconSizes = { 16, 32, 48, 64, 128, 256 };

            // WPF BitmapSource를 GDI+ Bitmap으로 변환
            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(currentImage));
                encoder.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);

                using (var originalBitmap = new System.Drawing.Bitmap(ms))
                using (var iconStream = new MemoryStream())
                {
                    var iconWriter = new BinaryWriter(iconStream);

                    // 아이콘 파일 헤더 작성
                    iconWriter.Write((short)0);      // Reserved
                    iconWriter.Write((short)1);      // Image type (1 = Icon)
                    iconWriter.Write((short)iconSizes.Length);  // Number of images

                    var imageDataList = new List<byte[]>();
                    long headerSize = 6 + (16 * iconSizes.Length);
                    long currentDataOffset = headerSize;

                    // 각 크기별 이미지 헤더 정보 작성
                    foreach (int size in iconSizes)
                    {
                        using (var resizedBitmap = new System.Drawing.Bitmap(size, size))
                        using (var g = System.Drawing.Graphics.FromImage(resizedBitmap))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(originalBitmap, 0, 0, size, size);

                            using (var ms2 = new MemoryStream())
                            {
                                resizedBitmap.Save(ms2, ImageFormat.Png);
                                var imageData = ms2.ToArray();
                                imageDataList.Add(imageData);

                                // 이미지 엔트리 작성
                                iconWriter.Write((byte)size);  // Width
                                iconWriter.Write((byte)size);  // Height
                                iconWriter.Write((byte)0);     // Color palette
                                iconWriter.Write((byte)0);     // Reserved
                                iconWriter.Write((short)1);    // Color planes
                                iconWriter.Write((short)32);   // Bits per pixel
                                iconWriter.Write((int)imageData.Length);  // Image size
                                iconWriter.Write((int)currentDataOffset); // Image offset

                                currentDataOffset += imageData.Length;
                            }
                        }
                    }

                    // 이미지 데이터 작성
                    foreach (var imageData in imageDataList)
                    {
                        iconWriter.Write(imageData);
                    }

                    // 파일로 저장
                    using (var fs = new FileStream(filepath, FileMode.Create))
                    {
                        iconStream.Seek(0, SeekOrigin.Begin);
                        iconStream.CopyTo(fs);
                    }
                }
            }

            statusText.Text = "아이콘 파일이 저장되었습니다.";
        }

        private void SaveAsWebP(string filepath)
        {
            try
            {
                // WPF BitmapSource를 GDI+ Bitmap으로 변환
                using (var ms = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(currentImage));
                    encoder.Save(ms);
                    ms.Seek(0, SeekOrigin.Begin);

                    using (var bitmap = new System.Drawing.Bitmap(ms))
                    {
                        // WebP로 저장
                        bitmap.Save(filepath, ImageFormat.Webp);
                    }
                }

                statusText.Text = "WebP 이미지가 저장되었습니다.";
            }
            catch (Exception ex)
            {
                throw new Exception($"WebP 저장 실패: {ex.Message}");
            }
        }

        private void btnFlipHorizontal_Click(object sender, RoutedEventArgs e)
        {
            if (currentImage == null) return;

            try
            {
                SaveToUndoStack();
                // 이미지를 좌우 반전
                var flippedBitmap = new TransformedBitmap(
                    currentImage,
                    new ScaleTransform(-1, 1)
                );

                // 반전된 이미지를 BitmapImage로 변환
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(flippedBitmap));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;

                    var newImage = new BitmapImage();
                    newImage.BeginInit();
                    newImage.CacheOption = BitmapCacheOption.OnLoad;
                    newImage.StreamSource = stream;
                    newImage.EndInit();
                    newImage.Freeze();

                    currentImage = newImage;
                    mainImage.Source = currentImage;
                    UpdateImageDisplay();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지 좌우반전 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            if (currentImage == null) return;

            try
            {
                SaveToUndoStack();
                // 이미지를 반시계방향으로 90도 회전
                var rotatedBitmap = new TransformedBitmap(
                    currentImage,
                    new RotateTransform(-90)
                );

                // 회전된 이미지를 BitmapImage로 변환
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rotatedBitmap));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;

                    var newImage = new BitmapImage();
                    newImage.BeginInit();
                    newImage.CacheOption = BitmapCacheOption.OnLoad;
                    newImage.StreamSource = stream;
                    newImage.EndInit();
                    newImage.Freeze();

                    currentImage = newImage;
                    mainImage.Source = currentImage;
                    UpdateImageDisplay();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지 왼쪽 회전 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRotateRight_Click(object sender, RoutedEventArgs e)
        {
            if (currentImage == null) return;

            try
            {
                SaveToUndoStack();
                // 이미지를 시계방향으로 90도 회전
                var rotatedBitmap = new TransformedBitmap(
                    currentImage,
                    new RotateTransform(90)
                );

                // 회전된 이미지를 BitmapImage로 변환
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rotatedBitmap));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;

                    var newImage = new BitmapImage();
                    newImage.BeginInit();
                    newImage.CacheOption = BitmapCacheOption.OnLoad;
                    newImage.StreamSource = stream;
                    newImage.EndInit();
                    newImage.Freeze();

                    currentImage = newImage;
                    mainImage.Source = currentImage;
                    UpdateImageDisplay();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지 오른쪽 회전 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCrop_Click(object sender, RoutedEventArgs e)
        {
            if (currentImage == null) return;

            if (!isCropping)
            {
                // 자르기 모드 시작
                isCropping = true;
                cropCanvas.Visibility = Visibility.Visible;
                cropCanvas.Width = mainImage.ActualWidth;
                cropCanvas.Height = mainImage.ActualHeight;
                statusText.Text = "자를 영역을 드래그하여 선택하세요. 선택 후 Ctrl+X를 누르거나 자르기 버튼을 다시 클릭하세요. ESC를 누르면 취소됩니다.";
            }
            else
            {
                // 선택 영역이 있을 경우 자르기 실행
                ApplyCrop();
            }
        }

        private void CropCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!isCropping) return;

            isDragging = true;
            cropStart = e.GetPosition(cropCanvas);
            cropSelector.Width = 0;
            cropSelector.Height = 0;
            Canvas.SetLeft(cropSelector, cropStart.X);
            Canvas.SetTop(cropSelector, cropStart.Y);
            cropSelector.Visibility = Visibility.Visible;
        }

        private void CropCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!isDragging) return;

            var pos = e.GetPosition(cropCanvas);
            var x = Math.Min(pos.X, cropStart.X);
            var y = Math.Min(pos.Y, cropStart.Y);
            var width = Math.Abs(pos.X - cropStart.X);
            var height = Math.Abs(pos.Y - cropStart.Y);

            Canvas.SetLeft(cropSelector, x);
            Canvas.SetTop(cropSelector, y);
            cropSelector.Width = width;
            cropSelector.Height = height;
        }

        private void CropCanvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isDragging = false;
        }

        private void ApplyCrop()
        {
            if (!isCropping || cropSelector.Visibility != Visibility.Visible) return;

            // 선택 영역이 너무 작은지 확인
            double scaleX = currentImage!.PixelWidth / mainImage.ActualWidth;
            double scaleY = currentImage.PixelHeight / mainImage.ActualHeight;
            int width = (int)(cropSelector.Width * scaleX);
            int height = (int)(cropSelector.Height * scaleY);

            if (width < 10 || height < 10)
            {
                MessageBox.Show("선택한 영역이 너무 작습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetCropSelector();
                return;
            }

            // 사용자에게 확인
            var result = MessageBox.Show("선택한 영역을 자르시겠습니까?", "확인", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                ResetCropSelector();
                return;
            }

            try
            {
                SaveToUndoStack();
                // 선택 영역의 좌표를 이미지 좌표로 변환
                int x = (int)(Canvas.GetLeft(cropSelector) * scaleX);
                int y = (int)(Canvas.GetTop(cropSelector) * scaleY);

                // 이미지 자르기
                var croppedBitmap = new CroppedBitmap(currentImage, new Int32Rect(x, y, width, height));

                // 잘린 이미지를 BitmapImage로 변환
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;

                    var newImage = new BitmapImage();
                    newImage.BeginInit();
                    newImage.CacheOption = BitmapCacheOption.OnLoad;
                    newImage.StreamSource = stream;
                    newImage.EndInit();
                    newImage.Freeze();

                    currentImage = newImage;
                    mainImage.Source = currentImage;
                    UpdateImageDisplay();
                }

                // 자르기 모드 종료
                EndCropping();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지 자르기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                EndCropping();
            }
        }

        private void ResetCropSelector()
        {
            // 선택 영역 초기화
            cropSelector.Visibility = Visibility.Collapsed;
            cropSelector.Width = 0;
            cropSelector.Height = 0;
            statusText.Text = "자를 영역을 드래그하여 선택하세요. 선택 후 Ctrl+X를 누르거나 자르기 버튼을 다시 클릭하세요. ESC를 누르면 취소됩니다.";
        }

        private void EndCropping()
        {
            isCropping = false;
            cropCanvas.Visibility = Visibility.Collapsed;
            cropSelector.Visibility = Visibility.Collapsed;
            statusText.Text = "자르기가 완료되었습니다.";
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == System.Windows.Input.Key.Escape && isCropping)
            {
                EndCropping();
                statusText.Text = "자르기가 취소되었습니다.";
            }
        }

        private void btnResize_Click(object sender, RoutedEventArgs e)
        {
            if (currentImage == null) return;

            var dialog = new ResizeDialog(currentImage.PixelWidth, currentImage.PixelHeight);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    SaveToUndoStack();
                    // 새로운 크기로 이미지 조정
                    var resizedImage = new TransformedBitmap(
                        currentImage,
                        new ScaleTransform(
                            (double)dialog.NewWidth / currentImage.PixelWidth,
                            (double)dialog.NewHeight / currentImage.PixelHeight
                        )
                    );

                    // 조정된 이미지를 BitmapImage로 변환
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(resizedImage));

                    using (var stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        stream.Position = 0;

                        var newImage = new BitmapImage();
                        newImage.BeginInit();
                        newImage.CacheOption = BitmapCacheOption.OnLoad;
                        newImage.StreamSource = stream;
                        newImage.EndInit();
                        newImage.Freeze();

                        currentImage = newImage;
                        mainImage.Source = currentImage;
                        UpdateImageDisplay();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"이미지 크기 조정 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentImagePath))
            {
                LoadImage(currentImagePath);
            }
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count > 0)
            {
                currentImage = undoStack.Pop();
                mainImage.Source = currentImage;
                UpdateImageDisplay();
                
                btnUndo.IsEnabled = undoStack.Count > 0;
                statusText.Text = "작업이 취소되었습니다.";
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.O:
                        btnOpen_Click(sender, e);
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.S:
                        btnSave_Click(sender, e);
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.H:
                        btnFlipHorizontal_Click(sender, e);
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.L:
                        btnRotateLeft_Click(sender, e);
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.R:
                        btnRotateRight_Click(sender, e);
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.X:
                        btnCrop_Click(sender, e);
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.E:
                        btnResize_Click(sender, e);
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.Z:
                        if (btnUndo.IsEnabled)
                        {
                            btnUndo_Click(sender, e);
                            e.Handled = true;
                        }
                        break;
                    case System.Windows.Input.Key.V:
                        PasteImageFromClipboard();
                        e.Handled = true;
                        break;
                }
            }
            else if (e.Key == System.Windows.Input.Key.F5)
            {
                btnReload_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.F1)
            {
                btnAbout_Click(sender, e);
                e.Handled = true;
            }
        }

        private void SaveToUndoStack()
        {
            if (currentImage == null) return;
            
            // 현재 이미지를 복사하여 스택에 저장
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(currentImage));
            
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Position = 0;
                
                var imageCopy = new BitmapImage();
                imageCopy.BeginInit();
                imageCopy.CacheOption = BitmapCacheOption.OnLoad;
                imageCopy.StreamSource = stream;
                imageCopy.EndInit();
                imageCopy.Freeze();
                
                undoStack.Push(imageCopy);
                
                // 최대 단계 수를 초과하면 가장 오래된 것 제거
                if (undoStack.Count > MAX_UNDO_STEPS)
                {
                    undoStack.Pop();
                }
            }
            
            btnUndo.IsEnabled = true;
        }

        private void MainImage_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (currentImage == null) return;

            // 마우스 위치 가져오기
            var pos = e.GetPosition(mainImage);

            // 이미지 상의 실제 좌표 계산
            double scaleX = currentImage.PixelWidth / mainImage.ActualWidth;
            double scaleY = currentImage.PixelHeight / mainImage.ActualHeight;
            int x = (int)(pos.X * scaleX);
            int y = (int)(pos.Y * scaleY);

            // 좌표가 이미지 범위 내에 있는지 확인
            if (x >= 0 && x < currentImage.PixelWidth && y >= 0 && y < currentImage.PixelHeight)
            {
                // 마우스 위치 표시
                txtMousePos.Text = $"위치: ({x}, {y})";

                // 픽셀 색상 가져오기
                try
                {
                    var cb = new CroppedBitmap(currentImage, new Int32Rect(x, y, 1, 1));
                    var pixels = new byte[4];
                    cb.CopyPixels(pixels, 4, 0);
                    
                    // BGRA 형식을 RGB로 변환하여 표시
                    txtColorInfo.Text = $"RGB: ({pixels[2]}, {pixels[1]}, {pixels[0]})";
                    // 색상 미리보기 업데이트
                    colorPreview.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(pixels[2], pixels[1], pixels[0]));
                }
                catch
                {
                    txtColorInfo.Text = "RGB: (-, -, -)";
                    colorPreview.Fill = null;
                }
            }
            else
            {
                txtMousePos.Text = "위치: (-, -)";
                txtColorInfo.Text = "RGB: (-, -, -)";
                colorPreview.Fill = null;
            }
        }

        private void MainImage_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            txtMousePos.Text = "위치: (-, -)";
            txtColorInfo.Text = "RGB: (-, -, -)";
            colorPreview.Fill = null;
        }

        private void PasteImageFromClipboard()
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    SaveToUndoStack();
                    
                    // 클립보드에서 이미지 가져오기
                    BitmapSource bitmapSource = Clipboard.GetImage();
                    
                    // BitmapImage로 변환
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                    using (var stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        stream.Position = 0;

                        var newImage = new BitmapImage();
                        newImage.BeginInit();
                        newImage.CacheOption = BitmapCacheOption.OnLoad;
                        newImage.StreamSource = stream;
                        newImage.EndInit();
                        newImage.Freeze();

                        currentImage = newImage;
                        mainImage.Source = currentImage;
                        currentImagePath = null; // 클립보드에서 붙여넣은 이미지는 파일 경로가 없음
                        UpdateImageDisplay();
                        statusText.Text = "클립보드의 이미지를 붙여넣었습니다.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지 붙여넣기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 