Imports AForge.Imaging.Filters
Imports AForge.Imaging





Public Class TrackingStructure

    Class ROIStructure


        Public Class DotsStructure
            Public Rect As Rectangle
            Public selected As Boolean
            Public pen As New Pen(Brushes.Green, 2)
            Public InitialX, InitialY As Integer
            Public IsMoved As Boolean
            Public Grabbed As Boolean

            Public Sub Move(Dx As Integer, Dy As Integer)
                Rect.X = InitialX + Dx
                Rect.Y = InitialY + Dy
            End Sub

        End Class



        Public Rect As Rectangle
        Public IsDragging As Boolean
        Public IsMade As Boolean
        Public IsMoved As Boolean
        Public Dots() As DotsStructure
        Public numDots As Integer
        Public ActiveDot As Integer
        Public pen As New Pen(Brushes.White, 2)
        Public InitialX, InitialY As Integer
        Public ClickX, ClickY, width, height As Integer

        Public Sub New(numDots As Integer)
            Me.numDots = numDots
            ReDim Dots(numDots)
            For i = 0 To numDots - 1
                Dots(i) = New DotsStructure
            Next

        End Sub

        Public Sub CorrectROI(W As Single, H As Single)

            ROIX = Math.Floor(W / Cursor.Width)
            ROIY = Math.Floor(H / Cursor.Height)

            width = ROIX * Cursor.Width
            height = ROIY * Cursor.Height
        End Sub

        Public Sub clear()
            IsMade = False
            IsDragging = False
        End Sub

        Public Sub Create()
            Rect = New Rectangle(ClickX, ClickY, width, height)

            InitialX = ClickX
            InitialY = ClickY

            Dim StepX, StepY As Integer
            StepX = Rect.Width / 3
            StepY = height / 3

            Dots(0).Rect = New Rectangle(Rect.X, Rect.Y, Cursor.Width, Cursor.Height)
            Dots(0).InitialX = Dots(0).Rect.X
            Dots(0).InitialY = Dots(0).Rect.Y

            Dots(1).Rect = New Rectangle(Rect.Right - Cursor.Width, Rect.Y, Cursor.Width, Cursor.Height)
            Dots(1).InitialX = Dots(1).Rect.X
            Dots(1).InitialY = Dots(1).Rect.Y

            Dots(2).Rect = New Rectangle(Rect.X, Rect.Bottom - Cursor.Height, Cursor.Width, Cursor.Height)
            Dots(2).InitialX = Dots(2).Rect.X
            Dots(2).InitialY = Dots(2).Rect.Y



            Dots(3).Rect = New Rectangle(Rect.Right - Cursor.Width, Rect.Bottom - Cursor.Height, Cursor.Width, Cursor.Height)
            Dots(3).InitialX = Dots(3).Rect.X
            Dots(3).InitialY = Dots(3).Rect.Y

            Dots(4).Rect = New Rectangle(Rect.Left + Rect.Width / 2, Rect.Top + Rect.Height / 2, Cursor.Width, Cursor.Height)
            Dots(4).InitialX = Dots(4).Rect.X
            Dots(4).InitialY = Dots(4).Rect.Y


            Dots(5).Rect = New Rectangle(Rect.Left + Rect.Width / 6, Rect.Top + Rect.Height / 6, Cursor.Width, Cursor.Height)
            Dots(5).InitialX = Dots(4).Rect.X
            Dots(5).InitialY = Dots(4).Rect.Y



            Dots(6).Rect = New Rectangle(Rect.Left + Rect.Width * (3 / 4), Rect.Top + Rect.Height * (3 / 4), Cursor.Width, Cursor.Height)
            Dots(6).InitialX = Dots(4).Rect.X
            Dots(6).InitialY = Dots(4).Rect.Y

            IsMade = True
        End Sub

        Public Sub Refresh()
            pen = New Pen(Brushes.White, 2)
            Pbox.Refresh()
            Pbox.CreateGraphics.DrawRectangle(pen, Rect)

            'For i = 0 To numDots - 1
            '    Pbox.CreateGraphics.DrawRectangle(Dots(i).pen, Dots(i).Rect)
            'Next
        End Sub

        Public Sub Move(Dx As Integer, Dy As Integer)
            Rect.X = InitialX + Dx
            Rect.Y = InitialY + Dy


            For i = 0 To numDots - 1
                Dots(i).Move(Dx, Dy)
            Next



        End Sub

        Public Sub Settle()
            InitialX = Rect.X
            InitialY = Rect.Y

            For i = 0 To numDots - 1
                Dots(i).InitialX = Dots(i).Rect.X
                Dots(i).InitialY = Dots(i).Rect.Y

            Next
        End Sub
    End Class


    Friend WithEvents pb As System.Windows.Forms.PictureBox
    Public Shared Pbox As PictureBox
    Public bmp As FastBMP
    Public Thumbnail As FastBMP
    Dim CursurClicked As Boolean
    Dim RecentX, RecentY As Single


    Public ROI As New ROIStructure(Setting.Gett("numdots"))


    'Public IsDragging As Boolean
    Public Shared ROIX, ROIY As Integer
    'Public ROImade As Boolean

    Dim T As New Timer
    Public Dots(2) As Rectangle
    Dim rectsIndex As Integer = 0
    'Position in pixels
    Dim X, Y As Single
    Dim XX, YY, ZZ As Single
    Public Shared Cursor As Button
    Dim Xmin, Xmax, Ymin, Ymax, Xrange, Yrange As Single
    Public Scanned() As Button

    Public Structure Points
        Public X As Single
        Public Y As Single
        Public Z As Single
    End Structure

    Public Sub New(ByRef Pb As PictureBox)
        UpdateCalibration()

        Cursor = New Button
        Pb.Size = New Size(Pb.Width, Pb.Width * (Yrange / Xrange))
        Pb.SizeMode = PictureBoxSizeMode.StretchImage
        Pbox = Pb

        Cursor.Width = Stage.FOVX * Pb.Width / Xrange
        Cursor.Height = Cursor.Width * Stage.FOVY / Stage.FOVX
        Cursor.FlatStyle = FlatStyle.Flat
        Cursor.ForeColor = Color.Yellow
        Cursor.BackColor = Color.Transparent

        Pbox.Controls.Add(Cursor)

        AddHandler T.Tick, AddressOf Update
        AddHandler Cursor.MouseDown, AddressOf GrabCursur
        AddHandler Cursor.MouseUp, AddressOf ReleaseCursur

        AddHandler Pb.DoubleClick, AddressOf Navigate
        AddHandler Pb.MouseDown, AddressOf MouseDown
        AddHandler Pb.MouseUp, AddressOf MouseUp
        AddHandler Pb.MouseMove, AddressOf MouseMove


        Pbox.Size = New Size(Pbox.Width, Pbox.Width * (Yrange / Xrange))
        clear()

        T.Interval = 100
        '  Enable()

    End Sub
    Public Sub UpdateCalibration()


        Xmin = Setting.Gett("Xmin")
        Ymin = Setting.Gett("Ymin")
        Xmax = Setting.Gett("Xmax")
        Ymax = Setting.Gett("Ymax")

        Xrange = Xmax - Xmin
        Yrange = Ymax - Ymin

        Setting.Sett("Xrange", Xrange)
        Setting.Sett("Yrange", Yrange)


    End Sub
    Public Sub MouseDown(sender As Object, e As MouseEventArgs)
        ROI.IsDragging = True
        ROI.ClickY = e.Y
        ROI.ClickX = e.X

        For i = 0 To ROI.numDots - 1
            If ROI.Dots(i).Rect.Contains(e.X, e.Y) Then
                ROI.Dots(i).Grabbed = True

            End If
        Next

    End Sub

    Public Sub MouseMove(sender As Object, e As MouseEventArgs)
        If ROI.IsDragging And Not ROI.IsMade Then

            ROI.CorrectROI(e.X - ROI.ClickX, e.Y - ROI.ClickY)
            Pbox.Refresh()
            Pbox.CreateGraphics.DrawRectangle(ROI.pen, ROI.ClickX, ROI.ClickY, ROI.width, ROI.height)

        End If


        If ROI.IsDragging And ROI.IsMade And ROI.Rect.Contains(e.X, e.Y) Then

            For i = 0 To ROI.numDots - 1
                If ROI.Dots(i).Grabbed Then
                    ROI.Dots(i).Move(e.X - ROI.ClickX, e.Y - ROI.ClickY)
                    ROI.Dots(i).IsMoved = True
                End If
            Next

            If Not (ROI.Dots(0).IsMoved Or ROI.Dots(1).IsMoved Or ROI.Dots(2).IsMoved Or ROI.Dots(3).IsMoved Or ROI.Dots(4).IsMoved Or ROI.Dots(5).IsMoved Or ROI.Dots(6).IsMoved) Then
                ROI.IsMoved = True
                ROI.Move(e.X - ROI.ClickX, e.Y - ROI.ClickY)
            End If

            ROI.Refresh()
        End If


    End Sub

    Public Sub MouseUp(sender As Object, e As MouseEventArgs)

        If ROI.width > 0 And ROI.height > 0 And Not ROI.IsMade Then
            ROI.Create()
            ROI.Refresh()
            Form1.SetScan()
        End If

        If ROI.IsMade Then
            If ROI.IsMoved Then ROI.IsMoved = False
            For i = 0 To ROI.numDots - 1
                If ROI.Dots(i).IsMoved Then ROI.Dots(i).IsMoved = False
                If ROI.Dots(i).Grabbed Then ROI.Dots(i).Grabbed = False
            Next
            ROI.Settle()

        End If

        ROI.IsDragging = False

    End Sub




    Public Sub AddScanned(W As Integer, H As Integer, XX As Single, YY As Single, address As String)
        Dim index As Integer = Scanned.GetLength(0)

        ReDim Preserve Scanned(index)
        Scanned(index) = New Button With {
        .Width = Cursor.Width * W,
        .Left = ConvertCoordinatetoPixels(XX, YY).X - Cursor.Width / 2,
        .Height = Cursor.Height * H,
        .Top = ConvertCoordinatetoPixels(XX, YY).Y - Cursor.Height / 2,
        .FlatStyle = FlatStyle.Flat,
        .ForeColor = Color.Yellow,
        .BackColor = Color.Transparent,
        .Tag = address
            }

        AddHandler Scanned(index).Click, AddressOf ScannedClick
        Pbox.Controls.Add(Scanned(index))

    End Sub
    Public Sub ScannedClick(sender As Object, e As EventArgs)
        If MsgBox(" Do you want to open the image?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
            ' open_Gimp(sender.tag)
        End If
    End Sub
    Public Sub UpdateBmp(bmpin As Bitmap)
        Tracking.bmp = New FastBMP(bmpin)
        Tracking.Pbox.Image = Tracking.bmp.bmp
    End Sub
    Public Sub Refreshbmp()
        Pbox.Image = bmp.bmp
    End Sub
    Public Sub clear()
        'bmp = New FastBMP(Pbox.Width, Pbox.Height, Imaging.PixelFormat.Format32bppArgb)


        ROI = New ROIStructure(Setting.Gett("numdots"))
        Pbox.Refresh()


        'Pbox.Image = bmp.bmp
        If Scanned IsNot Nothing Then
            For i = 0 To Scanned.GetUpperBound(0)
                Pbox.Controls.Remove(Scanned(i))
            Next
        End If

        ReDim Scanned(-1)

    End Sub

    Public Sub GrabCursur()
        CursurClicked = True

    End Sub

    Public Sub ReleaseCursur(sender As Object, e As MouseEventArgs)
        CursurClicked = False

        Stage.MoveAbsolute(Stage.Xaxe, (X + e.X - Cursor.Width / 2) * Xrange / Pbox.Width + Xmin)
        Stage.MoveAbsolute(Stage.Yaxe, (Y + e.Y - Cursor.Height / 2) * Yrange / Pbox.Height + Ymin)

    End Sub
    Private Sub Navigate(sender As Object, e As MouseEventArgs) Handles pb.MouseDoubleClick
        Form1.ExitEDOf()
        Stage.MoveAbsolute(Stage.Xaxe, ConvertPixeltoCoordinateX(e.X), False)
        Stage.MoveAbsolute(Stage.Yaxe, ConvertPixeltoCoordinateY(e.Y))
    End Sub
    Public Sub MovetoROIEdge()
        Stage.MoveAbsolute(Stage.Xaxe, ConvertPixeltoCoordinateX(ROI.Rect.X + Cursor.Width / 2))
        Stage.MoveAbsolute(Stage.Yaxe, ConvertPixeltoCoordinateY(ROI.Rect.Y + Cursor.Height / 2))
    End Sub


    Public Sub MovetoDots(i As Integer)
        Stage.MoveAbsolute(Stage.Xaxe, ConvertPixeltoCoordinateX(ROI.Dots(i).Rect.X + Cursor.Width / 2))
        Stage.MoveAbsolute(Stage.Yaxe, ConvertPixeltoCoordinateY(ROI.Dots(i).Rect.Y + Cursor.Height / 2))
    End Sub

    Public Sub MovetoNextDots()

        Stage.MoveAbsolute(Stage.Xaxe, ConvertPixeltoCoordinateX(ROI.Dots(ROI.ActiveDot).Rect.X + Cursor.Width / 2))
        Stage.MoveAbsolute(Stage.Yaxe, ConvertPixeltoCoordinateY(ROI.Dots(ROI.ActiveDot).Rect.Y + Cursor.Height / 2))
        ROI.ActiveDot += 1
        If ROI.ActiveDot = ROI.numDots Then ROI.ActiveDot = 0

    End Sub

    Public Sub Enable()
        T.Enabled = True
    End Sub

    Public Sub Disable()
        T.Enabled = False
    End Sub

    Public Sub Update()

        Cursor.Width = Stage.FOVX * Pbox.Width / Xrange
        Cursor.Height = Cursor.Width * Stage.FOVY / Stage.FOVX

        ' These are in mm

        'XX = stage.GetPosition(stage.Xaxe)
        'YY = stage.GetPosition(stage.Yaxe)

        XX = Stage.X
        YY = Stage.Y
        ZZ = Stage.Z


        '   ZZ = stage.GetPosition(stage.Zport)

        Form1.ToolStripStatusLabel1.Text = "X: " + XX.ToString
        Form1.ToolStripStatusLabel2.Text = "Y: " + YY.ToString
        Form1.ToolStripStatusLabel3.Text = "Z: " + ZZ.ToString
        ' Now conversion 

        X = ConvertCoordinatetoPixels(XX, YY).X
        Y = ConvertCoordinatetoPixels(XX, YY).Y


        '  If X <> RecentX Or Y <> RecentY Then

        Cursor.Top = Y - Cursor.Height / 2
        Cursor.Left = X - Cursor.Width / 2
        '   End If

        ROI.Refresh()

        RecentX = X
        RecentY = Y
    End Sub

    Private Function ConvertCoordinatetoPixels(XX As Single, YY As Single) As Points
        Dim P As New Points
        XX = XX - Xmin
        YY = YY - Ymin
        P.X = XX * Pbox.Width / Xrange
        P.Y = YY * Pbox.Height / Yrange

        Return P
    End Function

    Public Function ConvertPixeltoCoordinateX(XX As Single) As Single

        X = XX * Xrange / Pbox.Width + Xmin

        Return X
    End Function

    Public Sub MakeThumbnail(X As Single, y As Single, W As Integer, H As Integer)
        'bmp.Reset()
        'Thumbnail.MakeNew(bmp.width, bmp.height, Imaging.PixelFormat.Format24bppRgb)
        'Thumbnail.GR.DrawImage(bmp.bmp, 0, 0)

        'Thumbnail.GR.DrawRectangle(New Pen(Color.LawnGreen, 3), ConvertCoordinatetoPixels(X, y).X - CInt(Cursor.Width / 2), ConvertCoordinatetoPixels(X, y).Y - CInt(Cursor.Height / 2), Cursor.Width * W, Cursor.Height * H)
        'Thumbnail.GR.FillRectangle(New SolidBrush(Color.LawnGreen), ConvertCoordinatetoPixels(X, y).X - CInt(Cursor.Width / 2), ConvertCoordinatetoPixels(X, y).Y - CInt(Cursor.Height / 2), Cursor.Width * W, Cursor.Height * H)

    End Sub
    Public Function ConvertPixeltoCoordinateY(yy As Single) As Single

        Y = yy * Yrange / Pbox.Height + Ymin

        Return Y
        End Function
    End Class