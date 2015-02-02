Module PublicVariables
    Public appdata As String = My.Computer.FileSystem.SpecialDirectories.CurrentUserApplicationData + "\"

    Public Async Function Sleep(ms As Integer) As Task
        Await Task.Run( _
            Sub()
                System.Threading.Thread.Sleep(ms)
            End Sub)
    End Function
End Module
