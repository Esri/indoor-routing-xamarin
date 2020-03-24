using System;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    public class BottomSheetViewController : UIViewController
    {
        private enum State
        {
            minimized,
            partial,
            full
        };

        private const float fullViewYOffset = 100;
        private float minimizedYOffset = (float)UIScreen.MainScreen.Bounds.Height - 130f;

        private UIPanGestureRecognizer _gesture;

        public BottomSheetViewController()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            UIView.Animate(1, () => {
                MoveView(State.partial);
            });

            _gesture = new UIPanGestureRecognizer(HandleMoveView);
            this.View.AddGestureRecognizer(_gesture);
        }

    private void HandleMoveView(UIPanGestureRecognizer recognizer)
        {
            MoveView(recognizer);

            if (recognizer.State == UIGestureRecognizerState.Ended)
            {
                UIView.Animate(1, () =>
                {
                    if (recognizer.VelocityInView(View).Y >= 0)
                    {
                        MoveView(State.partial);
                    }
                    else
                    {
                        MoveView(State.full);
                    }
                });
            }
        }

        private void MoveView(UIPanGestureRecognizer recognizer)
        {
            var translation = recognizer.TranslationInView(View);
            var minY = View.Frame.Top;

            if (minY + translation.Y >= fullViewYOffset && minY + translation.Y <= minimizedYOffset)
            {
                View.Frame = new CoreGraphics.CGRect(0, minY + translation.Y, View.Frame.Width, View.Frame.Height);
                recognizer.SetTranslation(new CoreGraphics.CGPoint(0, 0), View);
            }
        }

        private void MoveView(State state)
        {
            float yPos = 0;

            switch (state)
            {
                case State.full:
                    yPos = fullViewYOffset;
                    break;
                case State.minimized:
                    yPos = minimizedYOffset;
                    break;
                case State.partial:
                    yPos = minimizedYOffset;
                    break;
            }

            View.Frame = new CoreGraphics.CGRect(0, yPos, View.Frame.Width, View.Frame.Height);
        }
    }
}