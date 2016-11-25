Class Application

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

    Sub New()
        ' Initialize the Parse client with your Application ID and .NET Key found on
        ' your Parse dashboard
        ParseClient.Initialize("YOUR APPLICATION ID", "YOUR .NET KEY")
    End Sub

    Private Async Sub Application_Startup(ByVal o As Object, ByVal e As StartupEventArgs) Handles Me.Startup
        Await ParseAnalytics.TrackAppOpenedAsync()
    End Sub

End Class
