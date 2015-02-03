Module PublicVariables

    Private _defaultDataPath As String = My.Computer.FileSystem.SpecialDirectories.MyDocuments + "\.blauncher"

    Public Property appdata As String
        Get
            If My.Settings.DataPath = "" Or Not IO.Directory.Exists(My.Settings.DataPath) Then
                If Not IO.Directory.Exists(_defaultDataPath) Then
                    IO.Directory.CreateDirectory(_defaultDataPath).Attributes = IO.FileAttributes.Hidden
                End If

                My.Settings.DataPath = _defaultDataPath
                My.Settings.Save()
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
