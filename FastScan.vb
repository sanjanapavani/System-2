Public Class FastScan
    Dim bytes() As Byte
    Public Sub New(X As Integer, Y As Integer, W As Integer, H As Integer)
        ReDim bytes(W * H * 3 - 1)
    End Sub
    Public Sub Scan()

    End Sub

    Public Sub ContinuousAcqusition()
        Camera.CaptureBmp()
    End Sub
End Class
