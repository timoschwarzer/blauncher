Imports System.Net
Imports HtmlAgilityPack
Imports System.Windows.Threading
Imports blauncher.PublicVariables
Imports Ionic.Zip
Imports System.ComponentModel
Imports System.Windows.Media.Animation

Class MainWindow

#Region "Vars"
    Dim AnimBase As New AnimationBase(Me)
    Const MaximumProgress As Integer = 100
    Public CheckArgs As Boolean = True
#End Region

#Region "Event Handler"
    Private Sub RootWindow_Loaded() Handles RootWindow.Loaded
        Opacity = 0

        Try
            If Not My.Settings.SettingsUpgraded Then
                My.Settings.Upgrade()
            End If
        Catch ex As Exception

        End Try

        If CheckArgs Then
            If Environment.GetCommandLineArgs().Count = 2 AndAlso Environment.GetCommandLineArgs()(1) = "-launch" Then
            Else
                Dim newSettingsWindow As New SettingsWindow
                newSettingsWindow.Show()
                Hide()
                Exit Sub
            End If
        End If

        For Each File As String In IO.Directory.GetFiles(appdata)
            Try
                If File.EndsWith(".zip") Then IO.File.Delete(File)
            Catch ex As Exception
            End Try
        Next

        AnimBase.Fade(0, 1, 300)



        Task.Run( _
            Sub()
                Dim updateSearchResult As New UpdateSearchResult

                Dim isNewerVersion As Boolean = True

                If My.Settings.UpdateAtLaunch Then
                    Try
                        If My.Settings.UseExperimentalVersions Then
                            updateSearchResult = NewCanaryVersionIsAvailable()
                        Else
                            updateSearchResult = NewReleaseVersionIsAvailable()
                        End If
                    Catch ex As Exception
                    End Try


                    If IO.File.Exists(appdata + "version.blnc") Then
                        If IO.File.ReadAllText(appdata + "version.blnc") = updateSearchResult.versionChk Then
                            isNewerVersion = False
                        End If
                    End If
                End If

                If isNewerVersion And My.Settings.UpdateAtLaunch Then
                    Try
                        UpdateAndStartBlender(updateSearchResult)
                    Catch ex As Exception
                        Dispatcher.Invoke( _
                        Sub()
                            VersionCodeLabel.Content = "no connection"
                        End Sub)
                        System.Threading.Thread.Sleep(1500)
                        LaunchBlender(updateSearchResult)
                    End Try
                Else

                    LaunchBlender(updateSearchResult)
                End If
            End Sub)
    End Sub
    Private Sub MainProgressBar_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles MainProgressBar.ValueChanged
        If BlueProgressBarGradientStop IsNot Nothing Then
            BlueProgressBarGradientStop.Offset = (MainProgressBar.Value / MainProgressBar.Maximum) - 0.001
        End If
    End Sub
    Private Sub BlenderLogoImage_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles BlenderLogoImage.MouseDown
        DragMove()
    End Sub
#End Region

#Region "Functions"
    Private Sub SetProgress(value As Integer, Optional maximum As Integer = MaximumProgress, Optional speed As Integer = 1000)
        Dispatcher.Invoke( _
            Sub()
                MainProgressBar.Maximum = maximum
                MainProgressBar.SetValueAnimated(value, speed)
            End Sub)
    End Sub
    Private Function NewCanaryVersionIsAvailable() As UpdateSearchResult
        Dim wc As New WebClient

        Dim html As String = wc.DownloadString(New Uri("https://builder.blender.org/download/"))

        Dim newHtmlDocument As New HtmlDocument
        newHtmlDocument.LoadHtml(html)

        Dim myUpdateSearchResult As New UpdateSearchResult

        Dim foundWin64Node As Boolean = False
        For Each TableRowNode As HtmlNode In newHtmlDocument.GetElementbyId("downloadtable").ChildNodes
            For Each TableDataNode As HtmlNode In TableRowNode.ChildNodes
                If TableDataNode.OriginalName = "td" Then
                    For Each ANode As HtmlNode In TableDataNode.ChildNodes
                        If ANode.OriginalName = "a" Then
                            Dim fileHref As String = ANode.Attributes("href").Value
                            Dim checkSum As String = ""

                            Debug.Print(fileHref)

                            If fileHref.StartsWith("blender-") And fileHref.EndsWith("-win64.zip") Then
                                checkSum = fileHref.Split("-")(2)

                                Dispatcher.Invoke( _
                                    Sub()
                                        VersionCodeLabel.Content = checkSum
                                        Dim VersionAnimBase As New AnimationBase(VersionLabelBorder)
                                        VersionAnimBase.Fade(1)

                                        myUpdateSearchResult.versionChk = checkSum
                                        myUpdateSearchResult.versionUrl = "https://builder.blender.org/download/" + fileHref
                                        myUpdateSearchResult.versionFile = fileHref.Remove(fileHref.Length - 4)
                                    End Sub)

                                foundWin64Node = True
                            End If

                            If foundWin64Node Then Exit For
                        End If
                    Next
                    If foundWin64Node Then Exit For
                End If
            Next
            If foundWin64Node Then Exit For
        Next

        Return myUpdateSearchResult
    End Function
    Private Function NewReleaseVersionIsAvailable() As UpdateSearchResult
        Dim wc As New WebClient

        Dim html As String = wc.DownloadString(New Uri("http://www.blender.org/download/"))

        Dim newHtmlDocument As New HtmlDocument
        newHtmlDocument.LoadHtml(html)

        Dim myUpdateSearchResult As New UpdateSearchResult

        Dim foundWin64Node As Boolean = False
        For Each WindowsNode As HtmlNode In newHtmlDocument.GetElementbyId("windows").ChildNodes
            If WindowsNode.GetAttributeValue("class", "") = "package" Then
                For Each ListNode As HtmlNode In WindowsNode.ChildNodes
                    If ListNode.GetAttributeValue("class", "") = "links list-bullets-none" Then
                        For Each ANode As HtmlNode In ListNode.ChildNodes
                            If ANode.OriginalName = "a" And _
                                ANode.GetAttributeValue("href", "").StartsWith("http://download.blender.org/release/") And _
                                ANode.GetAttributeValue("href", "").EndsWith("windows64.zip") Then

                                Dim fileHref As String = ANode.Attributes("href").Value
                                Dim checkSum As String = ""

                                Debug.Print(fileHref)

                                checkSum = fileHref.Split("-")(1)

                                Dispatcher.Invoke( _
                                    Sub()
                                        VersionCodeLabel.Content = checkSum
                                        Dim VersionAnimBase As New AnimationBase(VersionLabelBorder)
                                        VersionAnimBase.Fade(1)

                                        myUpdateSearchResult.versionChk = checkSum
                                        myUpdateSearchResult.versionUrl = fileHref
                                        Dim versionFileWithExtension As String = fileHref.Split("/")(fileHref.Split("/").Count - 1)
                                        myUpdateSearchResult.versionFile = versionFileWithExtension.Remove(versionFileWithExtension.Length - 4)

                                    End Sub)

                                foundWin64Node = True

                            End If
                        Next
                        If foundWin64Node Then Exit For
                    End If
                Next
                If foundWin64Node Then Exit For
            End If
            If foundWin64Node Then Exit For
        Next

        Return myUpdateSearchResult
    End Function
    Private Sub UpdateAndStartBlender(updateSearchResult As UpdateSearchResult)
        Dispatcher.Invoke( _
            Sub()
                ShowCancelButton()
            End Sub)

        'If IO.Directory.Exists(appdata + updateSearchResult.versionChk) AndAlso Not updateSearchResult.versionChk = "" Then
        '    My.Computer.FileSystem.WriteAllText(appdata + "version.blnc", updateSearchResult.versionChk, False)
        '    Dispatcher.Invoke( _
        '     Sub()
        '         LaunchBlender(updateSearchResult)
        '     End Sub)
        '    Exit Sub
        'End If

        Dim wc As New WebClient
        AddHandler wc.DownloadProgressChanged, _
            Sub(sender As Object, e As DownloadProgressChangedEventArgs)
                Dispatcher.Invoke( _
                    Sub()
                        MainProgressBar.Maximum = e.TotalBytesToReceive
                        MainProgressBar.Value = e.BytesReceived
                        VersionCodeLabel.Content = updateSearchResult.versionChk + " - " + e.ProgressPercentage.ToString + "%"
                    End Sub)
            End Sub

        AddHandler wc.DownloadFileCompleted, _
            Sub()
                Dispatcher.Invoke( _
                     Sub()
                         HideCancelButton()
                     End Sub)

                My.Computer.FileSystem.WriteAllText(appdata + "version.blnc", updateSearchResult.versionChk, False)

                Dim zip As New ZipFile(appdata + updateSearchResult.versionChk + ".zip")
                IO.Directory.CreateDirectory(appdata + updateSearchResult.versionChk)

                AddHandler zip.ExtractProgress, _
                                   Sub(sender As Object, e As ExtractProgressEventArgs)
                                       Dispatcher.Invoke( _
                                           Sub()
                                               If Not e.EntriesTotal < e.EntriesExtracted And Not e.EntriesTotal = 0 Then
                                                   VersionCodeLabel.Content = "Extracting... " + CInt((e.EntriesExtracted / e.EntriesTotal * 100)).ToString + "%"
                                               End If
                                           End Sub)
                                   End Sub

                Task.Run( _
                    Sub()
                        zip.ExtractAll(appdata + updateSearchResult.versionChk, ExtractExistingFileAction.OverwriteSilently)

                        Dispatcher.Invoke( _
                            Sub()
                                VersionCodeLabel.Content = "File extension..."
                            End Sub)

                        Dispatcher.Invoke( _
                            Sub()
                                RegisterBlenderFileExtension()
                                LaunchBlender(updateSearchResult)
                            End Sub)
                    End Sub)
            End Sub

        wc.DownloadFileAsync(New Uri(updateSearchResult.versionUrl), appdata + updateSearchResult.versionChk + ".zip")
    End Sub
    Private Sub LaunchBlender(updateSearchResult As UpdateSearchResult)
        Dim versionFileExists As Boolean = IO.File.Exists(appdata + "version.blnc")

        If versionFileExists AndAlso IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc")).Count > 0 AndAlso IO.File.Exists(IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc"))(0) + "\" + "\blender-app.exe") Then
            SetProgress(1, 1)

            Dispatcher.Invoke( _
                Sub()
                    VersionCodeLabel.Content = IO.File.ReadAllText(appdata + "version.blnc")
                End Sub)

            Task.Run( _
                Sub()
                    System.Diagnostics.Process.Start(IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc"))(0) + "\" + "\blender-app.exe")
                    System.Threading.Thread.Sleep(1000)
                    Dispatcher.Invoke(Sub() AnimBase.Fade(1, 0, 700))
                    System.Threading.Thread.Sleep(800)
                    Dispatcher.Invoke(Sub() Application.Current.Shutdown())
                End Sub)


        ElseIf versionFileExists AndAlso IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc")).Count > 0 AndAlso IO.File.Exists(IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc"))(0) + "\" + "\blender.exe") Then
            SetProgress(1, 1)

            Dispatcher.Invoke( _
                Sub()
                    VersionCodeLabel.Content = IO.File.ReadAllText(appdata + "version.blnc")
                End Sub)

            Task.Run( _
                Sub()
                    System.Diagnostics.Process.Start(IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc"))(0) + "\" + "\blender.exe")
                    System.Threading.Thread.Sleep(1000)
                    Dispatcher.Invoke(Sub() AnimBase.Fade(1, 0, 700))
                    System.Threading.Thread.Sleep(800)
                    Dispatcher.Invoke(Sub() Application.Current.Shutdown())
                End Sub)

        Else
            Dim DialogRes As MessageBoxResult = MessageBoxResult.None
            Dispatcher.Invoke( _
                Sub()
                    DialogRes = MessageBox.Show("Couldn't start blender. Either blender wasn't installed successfully or you haven't installed blender. Download Blender now?", "", MessageBoxButton.YesNo, MessageBoxImage.Error)
                End Sub)
            If DialogRes = MessageBoxResult.Yes Then
                Try

                    Dim newUpdateSearchResult As New UpdateSearchResult

                    If My.Settings.UseExperimentalVersions Then
                        newUpdateSearchResult = NewCanaryVersionIsAvailable()
                    Else
                        newUpdateSearchResult = NewReleaseVersionIsAvailable()
                    End If

                    UpdateAndStartBlender(newUpdateSearchResult)
                Catch ex As Exception
                    Dispatcher.Invoke( _
                        Sub()
                            MessageBox.Show("Couldn't download Blender. Do you have internet access?", "", MessageBoxButton.OK, MessageBoxImage.Error)
                            Application.Current.Shutdown()
                        End Sub)
                End Try
            Else
                Application.Current.Shutdown()
            End If
        End If
    End Sub
    Private Sub RegisterBlenderFileExtension()
        Dim versionFileExists As Boolean = IO.File.Exists(appdata + "version.blnc")

        VersionCodeLabel.Content = "File extension..."

        If versionFileExists AndAlso IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc")).Count > 0 AndAlso IO.File.Exists(IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc"))(0) + "\" + "\blender-app.exe") Then
            System.Diagnostics.Process.Start(IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc"))(0) + "\" + "\blender-app.exe", "-r").WaitForExit()


        ElseIf versionFileExists AndAlso IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc")).Count > 0 AndAlso IO.File.Exists(IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc"))(0) + "\" + "\blender.exe") Then
            System.Diagnostics.Process.Start(IO.Directory.GetDirectories(appdata + IO.File.ReadAllText(appdata + "version.blnc"))(0) + "\" + "\blender.exe", "-r").WaitForExit()


        Else
            Dim DialogRes As MessageBoxResult = MessageBoxResult.None
            Dispatcher.Invoke( _
                Sub()
                    DialogRes = MessageBox.Show("Couldn't register .blend file extension. Either blender wasn't installed successfully or you haven't installed blender. Download Blender now?", "", MessageBoxButton.YesNo, MessageBoxImage.Error)
                End Sub)
            If DialogRes = MessageBoxResult.Yes Then
                Try

                    Dim newUpdateSearchResult As New UpdateSearchResult

                    If My.Settings.UseExperimentalVersions Then
                        newUpdateSearchResult = NewCanaryVersionIsAvailable()
                    Else
                        newUpdateSearchResult = NewReleaseVersionIsAvailable()
                    End If

                    UpdateAndStartBlender(newUpdateSearchResult)
                Catch ex As Exception
                    Dispatcher.Invoke( _
                        Sub()
                            MessageBox.Show("Couldn't download Blender. Do you have internet access?", "", MessageBoxButton.OK, MessageBoxImage.Error)
                            Application.Current.Shutdown()
                        End Sub)
                End Try
            Else
                Application.Current.Shutdown()
            End If
        End If
    End Sub
#End Region

#Region "Structures"
    Public Structure UpdateSearchResult
        Public versionUrl As String
        Public versionChk As String
        Public versionFile As String
    End Structure
#End Region

#Region "CancelButton EventHandler"
    Private Sub CancelLabel_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles CancelLabel.MouseLeftButtonUp
        Application.Current.Shutdown()
    End Sub
    Private Sub CancelLabel_MouseEnter(sender As Object, e As MouseEventArgs) Handles CancelLabel.MouseEnter
        Dim scaleTransform As New ScaleTransform(1, 1)
        CancelLabel.RenderTransform = scaleTransform
        Dim anim As New DoubleAnimation(1.5, New Duration(New TimeSpan(0, 0, 0, 0, 300)))
        Dim pe As New PowerEase With {.EasingMode = EasingMode.EaseOut, .Power = 7}
        anim.EasingFunction = pe
        scaleTransform.BeginAnimation(Windows.Media.ScaleTransform.ScaleXProperty, anim)
        scaleTransform.BeginAnimation(Windows.Media.ScaleTransform.ScaleYProperty, anim)
    End Sub
    Private Sub CancelLabel_MouseLeave(sender As Object, e As MouseEventArgs) Handles CancelLabel.MouseLeave
        Dim scaleTransform As New ScaleTransform(1.5, 1.5)
        CancelLabel.RenderTransform = scaleTransform
        Dim anim As New DoubleAnimation(1, New Duration(New TimeSpan(0, 0, 0, 0, 300)))
        Dim pe As New PowerEase With {.EasingMode = EasingMode.EaseOut, .Power = 7}
        anim.EasingFunction = pe
        scaleTransform.BeginAnimation(Windows.Media.ScaleTransform.ScaleXProperty, anim)
        scaleTransform.BeginAnimation(Windows.Media.ScaleTransform.ScaleYProperty, anim)
    End Sub
    Private Sub ShowCancelButton()
        CancelLabel.IsEnabled = True
        Dim cancelButtonAnimBase As New AnimationBase(CancelLabel)
        cancelButtonAnimBase.Fade(1)

        Dim infoBorderAnimBase As New AnimationBase(VersionLabelBorder)
        infoBorderAnimBase.PropertyDoubleAnimation(Border.WidthProperty, 175 + 44)
    End Sub
    Private Sub HideCancelButton()
        CancelLabel.IsEnabled = False
        Dim cancelButtonAnimBase As New AnimationBase(CancelLabel)
        cancelButtonAnimBase.Fade(0)

        Dim infoBorderAnimBase As New AnimationBase(VersionLabelBorder)
        infoBorderAnimBase.PropertyDoubleAnimation(Border.WidthProperty, 175)
    End Sub
#End Region

End Class
