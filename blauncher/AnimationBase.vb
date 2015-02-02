Imports System.Windows.Media.Animation

Public Class AnimationBase


#Region "Variablen"
    Private _myControl As UIElement = Nothing
#End Region

#Region "Public Sub New()"
    Public Sub New(_myCtrl As UIElement)
        _myControl = _myCtrl
    End Sub
#End Region

#Region "Funktionen"
    Public Sub Fade([to] As Double, Optional duration As Integer = 300)
        Dim anim As New DoubleAnimation([to], New Duration(TimeSpan.FromMilliseconds(duration)))
        _myControl.BeginAnimation(UIElement.OpacityProperty, anim)
    End Sub
    Public Sub Fade([from] As Double, [to] As Double, Optional duration As Integer = 300)
        Dim anim As New DoubleAnimation([from], [to], New Duration(TimeSpan.FromMilliseconds(duration)))
        _myControl.BeginAnimation(UIElement.OpacityProperty, anim)
    End Sub
    Public Sub PropertyDoubleAnimation(dp As DependencyProperty, [to] As Double, Optional duration As Integer = 1000)
        Dim anim As New DoubleAnimation([to], New Duration(TimeSpan.FromMilliseconds(duration)))
        Dim pe As New PowerEase With {.EasingMode = EasingMode.EaseInOut, .Power = 10}
        anim.EasingFunction = pe
        _myControl.BeginAnimation(dp, anim)
    End Sub
#End Region


End Class
