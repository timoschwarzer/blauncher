Imports System.Windows.Media.Animation
Imports System.Net
Imports HtmlAgilityPack
Imports Ionic.Zip
Imports Microsoft.WindowsAPICodePack.Dialogs

Public Class SettingsWindow

#Region "Variablen"
    Private AnimBase As New AnimationBase(Me)
    Private aboutPoppedOut As Boolean = False
#End Region

#Region "Event Handler"
    Private Sub SettingsWindowX_Loaded(sender As Object, e As RoutedEventArgs) Handles SettingsWindowX.Loaded
        AnimBase.Fade(0, 1, 300)

        LoadSettings()
        GetVersions()

        For Each File As String In IO.Directory.GetFiles(appdata)
            Try
                If File.EndsWith(".zip") Then IO.File.Delete(File)
            Catch ex As Exception
            End Try
        Next

        AppVersionLabel.Content = "v" + My.Application.Info.Version.ToString

        DataPathTextBox.Text = appdata
    End Sub
    Private Sub UpdateNowButton_Click(sender As Object, e As RoutedEventArgs) Handles UpdateNowButton.Click
        UpdateNowButton.Visibility = Windows.Visibility.Hidden

        Task.Run( _
            Sub()
                Try
                    Dim updateSearchResult As UpdateSearchResult = Nothing

                    If My.Settings.UseExperimentalVersions Then
                        updateSearchResult = NewCanaryVersionIsAvailable()
                    Else
                        updateSearchResult = NewReleaseVersionIsAvailable()
                    End If

                    Dim isNewerVersion As Boolean = True
                    If IO.File.Exists(appdata + "version.blnc") Then
                        If IO.File.ReadAllText(appdata + "version.blnc") = updateSearchResult.versionChk Then
                            isNewerVersion = False
                        End If
                    End If

                    If isNewerVersion Then
                        UpdateAndStartBlender(updateSearchResult)

                    Else

                        Dispatcher.Invoke( _
                            Sub()
                                MessageBox.Show("Latest version is already installed.", "", MessageBoxButton.OK, MessageBoxImage.Information)
                                UpdateNowButton.Visibility = Windows.Visibility.Visible
                                MainProgressBar.Value = 0
                            End Sub)
                    End If
                Catch ex As Exception
                    Dispatcher.Invoke( _
                        Sub()
                            MessageBox.Show("Couldn't download Blender. Do you have internet access?", "", MessageBoxButton.OK, MessageBoxImage.Error)
                        End Sub)
                End Try
            End Sub)
    End Sub
    Private Sub ReleaseChannelRadioButton_Checked(sender As Object, e As RoutedEventArgs) Handles ReleaseChannelRadioButton.Checked
        My.Settings.UseExperimentalVersions = False
        My.Settings.Save()
    End Sub
    Private Sub ExperimentalChannelRadioButton_Checked(sender As Object, e As RoutedEventArgs) Handles ExperimentalChannelRadioButton.Checked
        My.Settings.UseExperimentalVersions = True
        My.Settings.Save()
    End Sub
    Private Sub AutoUpdateCheckBox_Checked(sender As Object, e As RoutedEventArgs) Handles AutoUpdateCheckBox.Checked, AutoUpdateCheckBox.Unchecked
        My.Settings.UpdateAtLaunch = CBool(AutoUpdateCheckBox.IsChecked)
        My.Settings.Save()
    End Sub
    Private Sub LaunchBlenderButton_Click(sender As Object, e As RoutedEventArgs) Handles LaunchBlenderButton.Click
        AnimBase.Fade(1, 0, 300)
        ShowInTaskbar = False
        Dim MainWindow As New MainWindow With {.CheckArgs = False}
        MainWindow.Show()
    End Sub
    Private Sub DeleteOldVersions_Click(sender As Object, e As RoutedEventArgs) Handles DeleteOldVersions.Click
        Dim counter As Integer = 0
        For Each Dir As String In IO.Directory.GetDirectories(appdata)
            Try
                If Not Dir.EndsWith("\" + IO.File.ReadAllText(appdata + "version.blnc")) Then IO.Directory.Delete(Dir, True) : counter += 1
            Catch ex As Exception
            End Try
        Next
        MessageBox.Show("Deleted " + counter.ToString + " unused version(s).", "", MessageBoxButton.OK, MessageBoxImage.Information)
    End Sub
    Private Sub DeleteAllVersions_Click(sender As Object, e As RoutedEventArgs) Handles DeleteAllVersions.Click
        If MessageBox.Show("Are you sure deleting all installed Blender versions?", "", MessageBoxButton.YesNo) Then
            Dim counter As Integer = 0
            For Each Dir As String In IO.Directory.GetDirectories(appdata)
                Try
                    My.Computer.FileSystem.DeleteDirectory(Dir, FileIO.DeleteDirectoryOption.DeleteAllContents)
                Catch
                End Try

                counter += 1
            Next
            Try
                IO.File.Delete(appdata + "version.blnc")
            Catch
            End Try
            MessageBox.Show("Deleted " + counter.ToString + " version(s).", "", MessageBoxButton.OK, MessageBoxImage.Information)
            GetVersions()
        End If
    End Sub
    Private Sub CreateBlenderShortcut_Click(sender As Object, e As RoutedEventArgs) Handles CreateBlenderShortcut.Click
        Dim exeDir As String = My.Application.Info.DirectoryPath
        Dim exePath As String = System.Reflection.Assembly.GetExecutingAssembly().CodeBase

        If CreateShortcut(My.Computer.FileSystem.SpecialDirectories.Desktop + "\Launch Blender.lnk", exePath, "-launch", "Launch Blender via bLauncher", exeDir, True) Then
            MessageBox.Show("Shortcut created on your desktop.", "", MessageBoxButton.OK, MessageBoxImage.Information)
        Else
            MessageBox.Show("Couldn't create shortcut." + vbNewLine + "You have to create a shortcut to bLauncher.exe -launch", "", MessageBoxButton.OK, MessageBoxImage.Error)
        End If
    End Sub
    Private Sub CreateBlenderShortcut_Copy_Click(sender As Object, e As RoutedEventArgs) Handles CreateBlenderShortcut_Copy.Click
        Dim exeDir As String = My.Application.Info.DirectoryPath
        Dim exePath As String = System.Reflection.Assembly.GetExecutingAssembly().CodeBase

        If CreateShortcut(My.Computer.FileSystem.SpecialDirectories.Desktop + "\bLauncher Settings.lnk", exePath, "", "bLauncher Settings", exeDir, False) Then
            MessageBox.Show("Shortcut created on your desktop.", "", MessageBoxButton.OK, MessageBoxImage.Information)
        Else
            MessageBox.Show("Couldn't create shortcut." + vbNewLine + "You have to create a shortcut to bLauncher.exe", "", MessageBoxButton.OK, MessageBoxImage.Error)
        End If
    End Sub
    Private Sub ChromeGrid_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles ChromeGrid.MouseLeftButtonDown
        DragMove()
    End Sub
    Private Sub setagonImage_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles setagonImage.MouseLeftButtonDown
        Process.Start("http://setagon.com")
    End Sub
    Private Sub blenderImage_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles blenderImage.MouseLeftButtonDown
        Process.Start("http://blender.org")
    End Sub
    Private Sub AboutButton_Click(sender As Object, e As RoutedEventArgs) Handles AboutButton.Click
        If aboutPoppedOut Then
            aboutPoppedOut = False
            AboutButton.Content = "About >"
            AnimBase.PropertyDoubleAnimation(Window.WidthProperty, 414)

        Else

            aboutPoppedOut = True
            AboutButton.Content = "< About"
            AnimBase.PropertyDoubleAnimation(Window.WidthProperty, 884)
        End If
    End Sub
    Private Sub DonateButton_Click(sender As Object, e As RoutedEventArgs) Handles DonateButton.Click
        MessageBox.Show("Hello!" + vbNewLine + _
                        "I'm Timo, a 15-years old student from Germany." + vbNewLine + _
                        "I make and made everything next to school as a hobby!" + vbNewLine + _
                        "I am VERY VERY HAPPY for every donation!" + vbNewLine + _
                        "You will now be redirected to my website. Just click the blue button with the PayPal logo to make a donation with PayPal " + vbNewLine + _
                        "or click on the Amazon-Button to buy things on Amazon!" + vbNewLine + vbNewLine + _
                        "THANK YOU!", "", MessageBoxButton.OK, MessageBoxImage.Information)
        Process.Start("http://setagon.com/donate")
    End Sub
    Private Sub DataPathChangeButton_Click(sender As Object, e As RoutedEventArgs) Handles DataPathChangeButton.Click
        Using fbd As New CommonOpenFileDialog With {.IsFolderPicker = True}
            If fbd.ShowDialog = CommonFileDialogResult.Ok Then
                appdata = fbd.FileName
                DataPathTextBox.Text = appdata
            End If
        End Using
    End Sub
#End Region

#Region "Funktionen"
    Private Sub LoadSettings()
        If My.Settings.UseExperimentalVersions Then
            ExperimentalChannelRadioButton.IsChecked = True
        Else
            ReleaseChannelRadioButton.IsChecked = True
        End If

        If My.Settings.UpdateAtLaunch Then
            AutoUpdateCheckBox.IsChecked = True
        Else
            AutoUpdateCheckBox.IsChecked = False
        End If
    End Sub

    Private Sub RegisterBlenderFileExtension()
        Dim versionFileExists As Boolean = IO.File.Exists(appdata + "version.blnc")

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
    Private Sub SetProgress(value As Integer, Optional maximum As Integer = 100, Optional speed As Integer = 1000)
        Dispatcher.Invoke( _
            Sub()
                MainProgressBar.Maximum = maximum
                MainProgressBar.SetValueAnimated(value, speed)
            End Sub)
    End Sub
    Private Function NewCanaryVersionIsAvailable() As UpdateSearchResult
        Dispatcher.Invoke( _
            Sub()
                ShowCancelButton()
            End Sub)

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

                                myUpdateSearchResult.versionChk = checkSum
                                myUpdateSearchResult.versionUrl = "https://builder.blender.org/download/" + fileHref
                                myUpdateSearchResult.versionFile = fileHref.Remove(fileHref.Length - 4)

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
        Dispatcher.Invoke( _
            Sub()
                ShowCancelButton()
            End Sub)

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


                                myUpdateSearchResult.versionChk = checkSum
                                myUpdateSearchResult.versionUrl = fileHref
                                Dim versionFileWithExtension As String = fileHref.Split("/")(fileHref.Split("/").Count - 1)
                                myUpdateSearchResult.versionFile = versionFileWithExtension.Remove(versionFileWithExtension.Length - 4)

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

        Dim wc As New WebClient
        AddHandler wc.DownloadProgressChanged, _
            Sub(sender As Object, e As DownloadProgressChangedEventArgs)
                Dispatcher.Invoke( _
                    Sub()
                        MainProgressBar.Maximum = e.TotalBytesToReceive
                        MainProgressBar.Value = e.BytesReceived
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
                                                   MainProgressBar.Maximum = 100
                                                   MainProgressBar.Value = CInt(e.EntriesExtracted / e.EntriesTotal * 100)
                                               End If
                                           End Sub)
                                   End Sub

                Task.Run( _
                    Sub()
                        zip.ExtractAll(appdata + updateSearchResult.versionChk, ExtractExistingFileAction.OverwriteSilently)

                        Dispatcher.Invoke( _
                           Sub()
                               RegisterBlenderFileExtension()
                               UpdateNowButton.Visibility = Windows.Visibility.Visible
                               ShowCancelButton()
                               MainProgressBar.Value = 0
                               GetVersions()
                           End Sub)
                    End Sub)
            End Sub

        wc.DownloadFileAsync(New Uri(updateSearchResult.versionUrl), appdata + updateSearchResult.versionChk + ".zip")
    End Sub
    Private Sub GetVersions()
        LatestVersionsLabel.Content = _
            "Searching..." + vbNewLine + _
            "Searching..." + vbNewLine + _
            "Searching..."

        Task.Run( _
            Sub()
                Try
                    Dim releaseUpdateSearchResult As UpdateSearchResult = NewReleaseVersionIsAvailable()
                    Dim experimentalUpdateSearchResult As UpdateSearchResult = NewCanaryVersionIsAvailable()

                    Dispatcher.Invoke( _
                        Sub()
                            LatestVersionsLabel.Content = _
                                releaseUpdateSearchResult.versionChk + vbNewLine + _
                                experimentalUpdateSearchResult.versionChk + vbNewLine + _
                                If(IO.File.Exists(appdata + "version.blnc"), IO.File.ReadAllText(appdata + "version.blnc"), "None")
                            UpdateNowButton.IsEnabled = True
                        End Sub)
                Catch ex As Exception
                    Dispatcher.Invoke( _
                        Sub()
                            LatestVersionsLabel.Content = _
                               "no connection" + vbNewLine + _
                               "no connection" + vbNewLine + _
                               If(IO.File.Exists(appdata + "version.blnc"), IO.File.ReadAllText(appdata + "version.blnc"), "None")
                            UpdateNowButton.IsEnabled = False
                        End Sub)
                End Try
            End Sub)
    End Sub
    Public Function CreateShortcut(ByVal sLinkFile As String, _
             ByVal sTargetFile As String, _
             Optional ByVal sArguments As String = "", _
             Optional ByVal sDescription As String = "", _
             Optional ByVal sWorkingDir As String = "", Optional useBlenderLogo As Boolean = False) As Boolean

        Try
            Dim oShell As New Shell32.Shell
            Dim oFolder As Shell32.Folder
            Dim oLink As Shell32.ShellLinkObject
            ' Ordner und Dateinamen extrahieren
            Dim sPath As String = sLinkFile.Substring(0, sLinkFile.LastIndexOf("\"))
            Dim sFile As String = sLinkFile.Substring(sLinkFile.LastIndexOf("\") + 1)
            ' Wichtig! Link-Datei erstellen (0 Bytes)
            Dim F As Short = FreeFile()
            FileOpen(F, sLinkFile, OpenMode.Output)
            FileClose(F)
            oFolder = oShell.NameSpace(sPath)
            oLink = oFolder.Items.Item(sFile).GetLink
            ' Eigenschaften der Verknüpfung
            With oLink
                If sArguments.Length > 0 Then .Arguments = sArguments
                If sDescription.Length > 0 Then .Description = sDescription
                If sWorkingDir.Length > 0 Then .WorkingDirectory = sWorkingDir
                .Path = sTargetFile

                If useBlenderLogo Then .SetIconLocation(My.Application.Info.DirectoryPath + "\blender-plain-logo.ico", 0)

                ' Verknüpfung speichern
                .Save()
            End With
            ' Objekte zerstören
            oLink = Nothing
            oFolder = Nothing
            oShell = Nothing
            Return True
        Catch ex As Exception
            ' Fehler! ggf. Link-Datei löschen, falls bereit erstellt

            MessageBox.Show("There was an error while creating the shortcut:" + vbNewLine + _
                            ex.Message)

            If System.IO.File.Exists(sLinkFile) Then Kill(sLinkFile)
            Return False
        End Try
    End Function
#End Region

#Region "CancelEvents"
    Private Sub CancelLabel_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles CancelLabel.MouseLeftButtonUp
        My.Settings.Save()
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
    End Sub
    Private Sub HideCancelButton()
        CancelLabel.IsEnabled = False
        Dim cancelButtonAnimBase As New AnimationBase(CancelLabel)
        cancelButtonAnimBase.Fade(0)
    End Sub
#End Region

#Region "Strukturen"
    Public Structure UpdateSearchResult
        Public versionUrl As String
        Public versionChk As String
        Public versionFile As String
    End Structure
#End Region

End Class
