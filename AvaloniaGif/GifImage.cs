using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Exception = System.Exception;

namespace AvaloniaGif
{
    public class GifImage : Control
    {
        public event Action Error;
        public static readonly StyledProperty<Uri> SourceUriProperty =
            AvaloniaProperty.Register<GifImage, Uri>(nameof(SourceUri));

        public static readonly StyledProperty<Stream> SourceStreamProperty =
            AvaloniaProperty.Register<GifImage, Stream>(nameof(SourceStream));

        public static readonly StyledProperty<IterationCount> IterationCountProperty =
            AvaloniaProperty.Register<GifImage, IterationCount>(nameof(IterationCount), IterationCount.Infinite);

        public static readonly StyledProperty<bool> AutoStartProperty =
            AvaloniaProperty.Register<GifImage, bool>(nameof(AutoStart));

        public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
            AvaloniaProperty.Register<GifImage, StretchDirection>(nameof(StretchDirection));

        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<GifImage, Stretch>(nameof(Stretch));

        private GifInstance _gifInstance;
        private RenderTargetBitmap _backingRtb;
        private bool _hasNewSource;
        private object? _newSource;
        private Stopwatch _stopwatch;

        static GifImage()
        {
            AffectsRender<GifImage>(SourceStreamProperty, SourceUriProperty, StretchProperty);
            AffectsArrange<GifImage>(SourceStreamProperty, SourceUriProperty, StretchProperty);
            AffectsMeasure<GifImage>(SourceStreamProperty, SourceUriProperty, StretchProperty);
        }
        
        public Uri SourceUri
        {
            get => GetValue(SourceUriProperty);
            set => SetValue(SourceUriProperty, value);
        }

        public Stream SourceStream
        {
            get => GetValue(SourceStreamProperty);
            set => SetValue(SourceStreamProperty, value);
        }

        public IterationCount IterationCount
        {
            get => GetValue(IterationCountProperty);
            set => SetValue(IterationCountProperty, value);
        }

        public bool AutoStart
        {
            get => GetValue(AutoStartProperty);
            set => SetValue(AutoStartProperty, value);
        }

        public StretchDirection StretchDirection
        {
            get => GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        private static void IterationCountChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is not GifImage image || e.NewValue is not IterationCount iterationCount)
                return;

            image.IterationCount = iterationCount;
        }

        public override void Render(DrawingContext context)
        {
            if (_hasNewSource)
            {
                StopAndDispose();
                try
                {
                    _gifInstance = new GifInstance(_newSource);
                }
                catch (Decoding.InvalidGifStreamException e)
                {
                    context.Dispose();
                    _hasNewSource = false;
                    if (Error is null) return;
                    Dispatcher.UIThread.Post(Error.Invoke, DispatcherPriority.Background);
                    return;
                }

                _gifInstance.IterationCount = IterationCount;
                _backingRtb = new RenderTargetBitmap(_gifInstance.GifPixelSize, new Vector(96, 96));
                _hasNewSource = false;

                _stopwatch ??= new Stopwatch();
                _stopwatch.Reset();


                return;
            }

            if (_gifInstance is null || (_gifInstance.CurrentCts?.IsCancellationRequested ?? true))
            {
                return;
            }

            if (AutoStart)
            {
                if (!_stopwatch.IsRunning)
                {
                    _stopwatch.Start();
                }
            }   

            var currentFrame = _gifInstance.ProcessFrameTime(_stopwatch.Elapsed);

            if (currentFrame != null && _backingRtb != null)
            {
                using var ctx = _backingRtb.CreateDrawingContext();
                var ts = new Rect(currentFrame.Size);
                ctx.DrawImage(currentFrame, ts, ts);
            }

            if (_backingRtb is null || !(Bounds.Width > 0) || !(Bounds.Height > 0)) return;
            var viewPort = new Rect(Bounds.Size);
            var sourceSize = _backingRtb.Size;

            var scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
            var scaledSize = sourceSize * scale;
            var destRect = viewPort
                .CenterRect(new Rect(scaledSize))
                .Intersect(viewPort);

            var sourceRect = new Rect(sourceSize)
                .CenterRect(new Rect(destRect.Size / scale));

            context.DrawImage(_backingRtb, sourceRect, destRect);
            Dispatcher.UIThread.Post(InvalidateMeasure, DispatcherPriority.Background);
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        }

        public void Start()
        {
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Reset();
            _stopwatch.Stop();
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var source = _backingRtb;
            var result = new Size();

            if (source != null)
            {
                result = Stretch.CalculateSize(availableSize, source.Size, StretchDirection);
            }

            return result;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var source = _backingRtb;

            if (source == null) return new Size();
            var sourceSize = source.Size;
            var result = Stretch.CalculateSize(finalSize, sourceSize);
            return result;

        }

        public void StopAndDispose()
        {
            _gifInstance?.Dispose();
            _backingRtb?.Dispose();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property != SourceUriProperty
                && e.Property != SourceStreamProperty
                && e.Property != IterationCountProperty
                && e.Property != AutoStartProperty)
            {
                return;
            }

            if (e.Property == IterationCountProperty)
            {
                IterationCountChanged(e);
            }

            var image = e.Sender as GifImage;

            if (image == null)
                return;

            if (e.NewValue is null)
            {
                return;
            }

            image._hasNewSource = true;
            image._newSource = e.NewValue;
            Dispatcher.UIThread.Post(image.InvalidateVisual, DispatcherPriority.Background);
        }
    }
}