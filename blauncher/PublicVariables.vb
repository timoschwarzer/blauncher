Imports Microsoft.WindowsAPICodePack.Dialogs

Module PublicVariables

    Private _defaultDataPath As String = My.Computer.FileSystem.SpecialDirectories.MyDocuments + "\.blauncher\"

    Public Property appdata As String
        Get
            If My.Settings.DataPath = "" Or Not IO.Directory.Exists(My.Settings.DataPath) Then
                Try
                    If Not IO.Directory.Exists(_defaultDataPath) Then
                        IO.Directory.CreateDirectory(_defaultDataPath).Attributes = IO.FileAttributes.Hidden
                    End If

                    appdata = _defaultDataPath
                Catch ex As Exception
                    MessageBox.Show("Please tell me the directory where I should save the different blender versions.", "", MessageBoxButton.OK, MessageBoxImage.Information)
                    Using fbd As New CommonOpenFileDialog With {.IsFolderPicker = True}
                        If fbd.ShowDialog = CommonFileDialogResult.Ok Then
                            appdata = fbd.FileName
                        Else
                            Application.Current.Shutdown()
                        End If
                    End Using
                End Try
            End If

            Return My.Settings.DataPath
        End Get
        Set(value As String)
            If Not value.EndsWith("/") Or Not value.EndsWith("\") Then
                value += "\"
            End If
            If IO.Directory.Exists(value) Then
                My.Settings.DataPath = value
                My.Settings.Save()
            Else
                Throw New Exception("Dir " + value + " does not exist")
            End If
        End Set
    End Property

    Public Async Function Sleep(ms As Integer) As Task
        Await Task.Run( _
            Sub()
                System.Threading.Thread.Sleep(ms)
            End Sub)
    End Function
End Module
