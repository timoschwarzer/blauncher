Imports System.Windows.Media.Animation

Module AnimationExtensions
    <System.Runtime.CompilerServices.Extension> _
    Public Sub SetValueAnimated(p As System.Windows.Controls.ProgressBar, val As Double, Optional speed As Integer = 900)
        Dim da As New System.Windows.Media.Animation.DoubleAnimation()
        da.[To] = val
        da.EasingFunction = New PowerEase With {.EasingMode = EasingMode.EaseInOut, .Power = 5}
        da.Duration = New System.Windows.Duration(New System.TimeSpan(0, 0, 0, 0, speed))
        p.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, da)
    End Sub
End Module
