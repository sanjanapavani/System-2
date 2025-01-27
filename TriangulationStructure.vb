

Public Class TriangulationStructure
    Public ROI As Rectangle
    Public Sub New(OffsetX, OffsetY, RoiX, ROiY)
        ROI = New Rectangle(OffsetX, OffsetY, RoiX, ROiY)
    End Sub

    Public Sub Initialize()
        Camera.Flatfield(0)

        Camera.SetDataMode(Colortype.Grey)
        '  Camera.SetROI(ROI.X, ROI.Y, ROI.Width, ROI.Height)
    End Sub

    Public Sub release()
        Camera.ReSetROI()
        Camera.SetDataMode(Colortype.RGB)
        Camera.Flatfield(1)
    End Sub


    Public Sub Capture(y As Single, X As Integer)

        Dim j As Integer
        For loopx = 1 To X
            For i = 1 To y
                Camera.Capture()
                Stage.MoveRelative(Stage.Yaxe, 0.2, False)
                j += 1
                SaveSinglePageTiff("c:\temp\oskol\" + (j + 1000).ToString + ".tif", Camera.Bytes, ROI.Width, ROI.Height)
            Next
            Stage.MoveRelative(Stage.Yaxe, -y * 0.2, False)
            Stage.MoveRelative(Stage.Xaxe, -Stage.FOVX, False)
        Next
        Stage.MoveRelative(Stage.Xaxe, Stage.FOVX * X, False)
    End Sub

End Class
