using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Verion.Apresentacao.Avalonia.Inputs;
using Verion.Apresentacao.Avalonia.Inputs.TextBoxTypes.StructNullTextBox;
using Verion.Presentation.View;
using Verion.Presentation.View.UseCase;

namespace Verion.Treinamento.DapperDemo.View.Components
{
    public abstract class UserControlMobile<TModelo, TInput, TResult> : UserControl, PresenterBase<TModelo, TInput, TResult>,
        IDisposable
        where TModelo : PresentationModelBase<TInput, TResult>
    {
        private static Point textBoxPosition;

        protected UserControlMobile()
        {
            ModeloApresentacao = PreviewMobile.Container!.Resolve<TModelo>();
            DataContext = ModeloApresentacao;
            Background = Brushes.Black;
        }

        public double DistanceToMoveWithKeyboard => 0.2d;

        public TModelo ModeloApresentacao { get; }

        public virtual Task<TResult> RunAsync(TInput input, PresentationExecutionContext context, CancellationToken cancellationToken = default)
        {
            return ModeloApresentacao.RunAsync(input, context, cancellationToken);
        }

        public virtual void Dispose()
        {
        }

        public void OnFocusGetPositionToMoveWithKeyboard(object? sender, FocusChangedEventArgs e)
        {
            if (sender is not Control control)
                return;

            textBoxPosition = GetAbsolutePosition(control);
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            var pane = TopLevel.GetTopLevel(this)?.InputPane;
            if (pane != null)
            {
                pane.StateChanged += OnInputPaneStateChanged;
            }

            var inputElements = this.GetVisualDescendants().OfType<Control>()
                .Where(control => control is TextBox or NumericStructNullEntryBase or VTextBoxWithLabel);

            foreach (var inputElement in inputElements)
            {
                inputElement.GotFocus += OnFocusGetPositionToMoveWithKeyboard;
            }
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            var pane = TopLevel.GetTopLevel(this)?.InputPane;
            if (pane != null)
            {
                pane.StateChanged -= OnInputPaneStateChanged;
            }

            var inputElements = this.GetVisualDescendants().OfType<Control>()
                .Where(control => control is TextBox or NumericStructNullEntryBase or VTextBoxWithLabel);

            foreach (var inputElement in inputElements)
            {
                inputElement.GotFocus -= OnFocusGetPositionToMoveWithKeyboard;
            }
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            Dispose();
        }

        private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
        {
            if (Content is not Control content)
                return;

            var newTranslationHeight = (Bounds.Height / 2) - (textBoxPosition.Y * Bounds.Height / 1560) - (Bounds.Height * DistanceToMoveWithKeyboard);

            if (newTranslationHeight > 0)
                return;

            content.RenderTransform = e.NewState == InputPaneState.Open ? new TranslateTransform(0, Math.Max(newTranslationHeight, -Bounds.Height / 2)) : null;
        }

        private Point GetAbsolutePosition(Control? control)
        {
            var position = new Point(0, 0);
            while (control != null)
            {
                position = position.WithX(position.X + control.Bounds.X)
                    .WithY(position.Y + control.Bounds.Y);

                control = control.Parent as Control;
            }

            return position;
        }
    }
}