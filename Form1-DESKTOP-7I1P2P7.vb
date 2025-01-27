Imports System.ComponentModel
Imports System.IO
Imports AForge.Imaging.Filters
Imports Microsoft.VisualBasic.Devices
Imports AForge.Imaging
Imports System.Threading
Imports System.Windows.Forms.DataVisualization.Charting
Imports MathNet.Numerics
Imports MVStitchintLibrary
Imports System.Drawing.Imaging

Public Class Form1


    Public LEDcontroller As Relay
    Dim IsDragging As Boolean
    Dim AutoFocus As FocusStructure
    Dim Slideloaded As Boolean
    Dim StopAlign As Boolean
    Dim panel As Integer
    Dim Focusing As Boolean
    Dim Filenames() As String
    Dim Scanning As Boolean
    Dim fileN As Integer
    Dim ScanOverlap As Integer = 100

    Const WhiteLED = 2
    Const BlueLED = 1
    Const PreviewLED = 3
    Dim T As test
    Dim Dehaze As DehazeClass


    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'T = New test(60, Pbar)
        'T.EstimateProfile()
        'PictureBox_Preview.Image = T.ZmapBmp.bmp
        'Exit Sub

        'Dehaze = New DehazeClass(2048, 2048, 0.1, 0.8)
        'Dim bytes() As Byte
        'Dim bmp As New Bitmap(2048, 2048, Imaging.PixelFormat.Format24bppRgb)
        'BitmapToBytes(New Bitmap("c:\temp\A.jpg"), bytes)
        'Dehaze.Apply(bytes)
        'byteToBitmap(bytes, bmp)
        'bmp.Save("c:\temp\B.bmp", ImageFormat.Bmp)
        'End


        LEDcontroller = New Relay
        LEDcontroller.SetRelays(WhiteLED, False)

        LEDcontroller.SetRelays(BlueLED, False)
        LEDcontroller.SetRelays(PreviewLED, False)


        Preview = New PreviewVimba(40, 0.1, Pbar)
        TextBox_PrevieEXp.Text = Setting.Gett("PREVIEWEXP")
        TextBox_PreviewFocus.Text = Setting.Gett("PREVIEWFOCUS")
        Preview.SetExposure(TextBox_PrevieEXp.Text)

        Camera = New XimeaXIq
        '        EDF = New ExtendedDepth5(Camera.W, Camera.H, 0.25, False)
        ZEDOF = New ZstackStructure(Camera.W, Camera.H, Setting.Gett("ZSTACRRANGE"), Setting.Gett("ZSTACKSTEPS"), Setting.Gett("ZSTACKSCALE"))
        Zprofiler = New ZstackStructure(Camera.W, Camera.H, Setting.Gett("ZSTACRRANGE"), Setting.Gett("ZSTACKSTEPS"), 4)
        TextBox21.Text = Setting.Gett("ZSTACRRANGE")
        TextBox22.Text = Setting.Gett("ZSTACKSTEPS")
        TextBox23.Text = Setting.Gett("ZSTACKSCALE")
        'Triangle = New TriangulationStructure(340, 1078, 2600, 700)
        If Camera.status Then
            TextBox_exposure.Text = Camera.exp
            'AutoFocus = New FocusStructure(2, 0.1, 4)
            Display = New ImageDisplay(Camera.W, Camera.H, Chart1)
            Display.imagetype = ImagetypeEnum.Brightfield
        End If

        Stage = New ZaberASCII(Setting.Gett("FOVX"), Setting.Gett("FOVY"))
        TextBox_FOVX.Text = Stage.FOVX
        TextBox_FOVY.Text = Stage.FOVY

        'Piezo = New EO(10)


        TextBoxGain.Text = Setting.Gett("Gain")
        TextBox_GainB.Text = Setting.Gett("GainB")
        TextBox_GainG.Text = Setting.Gett("GainG")
        TextBox_GainR.Text = Setting.Gett("GainR")

        TextBoxGY.Text = Setting.Gett("GAMMAY")
        TextBoxGC.Text = Setting.Gett("GAMMAC")

        TextBox_exposure.Text = Setting.Gett("exposure")


        If Camera.status Then

            'Camera.SetFlatField("ff.tif", "dark.tif")
            Camera.Flatfield(0)
            GoLive()
            ArrangeControls(10)

            ReDim ScanUnits(ScanBufferSize - 1)

            For b = 0 To ScanBufferSize - 1
                ScanUnits(b) = New ScanUnit(Camera.W, Camera.H, TextBox21.Text, TextBox22.Text, TextBox23.Text, b)
            Next b

        End If

        Preview.Scale = Preview.ROI_W / PictureBox_Preview.Width


        'GetPreview(True)
    End Sub


    Sub ArrangeControls(d As Integer)
        Dim scale As Single = 0.34 * 2708 / 2048

        PictureBox0.Width = Display.Width * scale
        PictureBox0.Height = Display.Height * scale
        PictureBox0.SizeMode = PictureBoxSizeMode.Zoom
        PictureBox0.Top = TabControl1.Top + d
        PictureBox0.Left = TabControl1.Left + TabControl1.Width - d



        TabControl1.Width = Display.Width * scale + 2 * d
        TabControl1.Height = Display.Height * scale + 2 * d

        TabControl2.Left = TabControl1.Width + d
        TabControl2.Width = Me.ClientSize.Width - TabControl1.Width - d
        TabControl2.Height = TabControl1.Height



        PictureBox_Preview.Left = d
        PictureBox_Preview.Top = d
        PictureBox_Preview.Width = (TabControl2.Width - 2 * d)

        Tracking = New TrackingStructure(PictureBox_Preview)
        Tracking.Update()

        GroupBox3.Left = PictureBox_Preview.Left
        GroupBox3.Top = PictureBox_Preview.Top + PictureBox_Preview.Height + d
        TabControl_Settings.Top = d
        TabControl_Settings.Left = d + d
        Chart1.Left = GroupBox3.Left + GroupBox3.Width + d
        Chart1.Top = GroupBox3.Top
        Chart1.Height = GroupBox3.Height

        ListBox1.Left = Chart1.Left + Chart1.Width + d
        ListBox1.Top = GroupBox3.Top
        ListBox1.Height = GroupBox3.Height
        Button_GIMP.Left = ListBox1.Left + ListBox1.Width + d
        Button_GIMP.Top = ListBox1.Top
        Button_Luigi.Left = Button_GIMP.Left
        Button_Luigi.Top = Button_GIMP.Top + Button_GIMP.Height

        Button_Sedeen.Left = Button_GIMP.Left
        Button_Sedeen.Top = Button_Luigi.Top + Button_Luigi.Height

        GroupBox2.Top = Button_Sedeen.Top
        GroupBox2.Left = Button_Sedeen.Left + Button_Sedeen.Width + d




    End Sub

    Public Sub ChangeExposure()



        Camera.exp = Val(TextBox_exposure.Text)

        Select Case Display.imagetype
            Case ImagetypeEnum.Brightfield
                Setting.Sett("EXPOSUREB", Camera.exp)
            Case ImagetypeEnum.Fluorescence
                Setting.Sett("EXPOSUREF", Camera.exp)
            Case ImagetypeEnum.MUSE
                Setting.Sett("EXPOSUREM", Camera.exp)
        End Select
        Setting.Sett("EXPOSURE", Camera.exp)

        Camera.ExposureChanged = 0

        'Do Until Camera.ExposureChanged = False

        'Loop
        'Display.AdjustBrightness()
    End Sub



    Public Sub GoLive()


        If (Camera.exp + Camera.readout_time) < 50 Then Timer1.Interval = 50 Else Timer1.Interval = Camera.exp + Camera.readout_time
        Camera.busy = False
        Camera.Dostop = False
        Timer1.Start()

        'Dim Thread1 As New System.Threading.Thread(AddressOf Live)
        'Thread1.Start()


    End Sub

    Public Sub ExitLive()
        'If Camera.status = False Then Exit Sub
        Timer1.Stop()

        Camera.Dostop = True
        Application.DoEvents()


    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick

        CaptureLive()


    End Sub

    Public Sub CaptureLive()

        If Camera.Dostop Then Exit Sub
        If Camera.busy Then Exit Sub

        Camera.busy = True


        If Camera.ExposureChanged = 0 Then Camera.SetExposure() : Display.AdjustBrightness() : Camera.ExposureChanged = 1
        If Display.RequestIbIc = 0 Then Camera.ResetMatrix() : Display.RequestIbIc = 1
        Camera.capture() : Display.MakePreview(Camera.Bytes, True)

        If CheckBoxLED.Checked Then PictureBox0.Image = Display.BmpPreview(Display.f).bmp Else PictureBox0.Image = Display.EmptyPreview.bmp


        Application.DoEvents()
        If Camera.Dostop Then Exit Sub

        Display.MakeHistogram()
        Display.PlotHistogram()
        Application.DoEvents()
        Camera.busy = False

    End Sub
    Private Sub Button_right_Click(sender As Object, e As EventArgs) Handles Button_right.Click

        Stage.MoveRelative(Stage.Xaxe, -Stage.FOVX)
        ExitEDOf()
    End Sub

    Private Sub Button_left_Click(sender As Object, e As EventArgs) Handles Button_left.Click

        Stage.MoveRelative(Stage.Xaxe, Stage.FOVX)
        ExitEDOf()
    End Sub

    Private Sub Button_top_Click(sender As Object, e As EventArgs) Handles Button_top.Click

        Stage.MoveRelative(Stage.Yaxe, Stage.FOVY)
        ExitEDOf()
    End Sub

    Private Sub Button_bottom_Click(sender As Object, e As EventArgs) Handles Button_bottom.Click

        Stage.MoveRelative(Stage.Yaxe, -Stage.FOVY)
        ExitEDOf()
    End Sub

    Private Sub Button_adjustBrightness_Click(sender As Object, e As EventArgs) Handles Button_adjustBrightness.Click

        Display.AdjustBrightness()


    End Sub

    Public Sub ExitEDOf()

        If Display.imagetype = ImagetypeEnum.EDF_Brightfield Then TabControl1.SelectedIndex = 0
        If Display.imagetype = ImagetypeEnum.EDF_Fluorescence Then TabControl1.SelectedIndex = 1
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        ExitLive()

        Stage.MoveAbsolute(Stage.Zaxe, 25)
        Preview.StopPreview()
        LEDcontroller.LED_OFF()
    End Sub

    Private Sub RadioButton_zoom_in_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton_zoom_in.CheckedChanged
        If RadioButton_zoom_in.Checked Then
            Display.zoom = True
            PictureBox0.SizeMode = PictureBoxSizeMode.CenterImage


        Else
            PictureBox0.SizeMode = PictureBoxSizeMode.Zoom


        End If
    End Sub

    Private Sub TextBox3_TextChanged(sender As Object, e As EventArgs) Handles TextBox3.TextChanged

    End Sub

    Private Sub TextBox3_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox3.KeyDown
        If e.KeyCode = Keys.Return Then

            Stage.MoveRelative(Stage.Zaxe, Val(TextBox3.Text))

        End If
    End Sub



    Private Sub TextBox_GainR_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_GainR.KeyDown
        If e.KeyCode = Keys.Return Then
            Try
                Display.SetColorGain(Val(TextBox_GainR.Text), Val(TextBox_GainG.Text), Val(TextBox_GainB.Text), Display.imagetype)
            Catch ex As Exception

            End Try

        End If
    End Sub

    Private Sub TextBox_GainG_TextChanged(sender As Object, e As EventArgs) Handles TextBox_GainG.TextChanged

    End Sub

    Private Sub TextBox_GainG_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_GainG.KeyDown
        If e.KeyCode = Keys.Return Then
            Try
                Display.SetColorGain(Val(TextBox_GainR.Text), Val(TextBox_GainG.Text), Val(TextBox_GainB.Text), Display.imagetype)
            Catch ex As Exception

            End Try

        End If
    End Sub



    Private Sub TextBox_GainB_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_GainB.KeyDown
        If e.KeyCode = Keys.Return Then
            Try
                Display.SetColorGain(Val(TextBox_GainR.Text), Val(TextBox_GainG.Text), Val(TextBox_GainB.Text), Display.imagetype)
            Catch ex As Exception

            End Try

        End If
    End Sub

    Private Sub TextBoxGain_TextChanged(sender As Object, e As EventArgs) Handles TextBoxGain.TextChanged

    End Sub

    Private Sub TextBoxGain_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBoxGain.KeyDown
        If e.KeyCode = Keys.Return Then
            Camera.setGain(Val(TextBoxGain.Text))
        End If
    End Sub



    Private Sub TabControl1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles TabControl1.SelectedIndexChanged

        ExitLive()


        If TabControl1.SelectedIndex = 0 Then


            Display.imagetype = ImagetypeEnum.Brightfield

            Camera.SetFlatField("ff.tif", "dark.tif")
            TextBox_exposure.Text = Setting.Gett("Exposureb")
            TextBox_GainB.Text = Setting.Gett("GainB")
            TextBox_GainG.Text = Setting.Gett("GainG")
            TextBox_GainR.Text = Setting.Gett("GainR")
            Display.SetColorGain(Setting.Gett("GainR"), Setting.Gett("GainG"), Setting.Gett("GainB"), ImagetypeEnum.Brightfield)
            UpdateLED(CheckBoxLED.Checked)
            ChangeExposure()
            GoLive()
        End If

        If TabControl1.SelectedIndex = 1 Then

            Display.imagetype = ImagetypeEnum.Fluorescence
            Camera.SetFlatField("ff_FiBi.tif", "dark.tif")
            TextBox_exposure.Text = Setting.Gett("Exposuref")
            TextBox_GainB.Text = Setting.Gett("GainB_FiBi")
            TextBox_GainG.Text = Setting.Gett("GainG_FiBi")
            TextBox_GainR.Text = Setting.Gett("GainR_FiBi")
            Display.SetColorGain(Setting.Gett("GainR_FiBi"), Setting.Gett("GainG_FiBi"), Setting.Gett("GainB_FiBi"), ImagetypeEnum.Fluorescence)
            UpdateLED(CheckBoxLED.Checked)
            ChangeExposure()
            GoLive()
        End If


        If TabControl1.SelectedIndex = 2 Then
            ExitLive()
            If Display.imagetype = ImagetypeEnum.Fluorescence Then Display.imagetype = ImagetypeEnum.EDF_Fluorescence
            If Display.imagetype = ImagetypeEnum.Brightfield Then Display.imagetype = ImagetypeEnum.EDF_Brightfield

            Dim ccMatrix As Single = Camera.CCMAtrix
            Camera.ResetMatrix()


            ZEDOF.Acquire(True, 1)
            'ZEDOF.Acquire()
            Do Until ZEDOF.WrapUpDone

            Loop
            Dim bmp As New Bitmap(Camera.W, Camera.H, Imaging.PixelFormat.Format24bppRgb)
            byteToBitmap(ZEDOF.OutputBytes, bmp)

            Display.ApplyBrightness(ZEDOF.OutputBytes, ccMatrix, bmp)
            PictureBox0.Image = bmp
            UpdateLED(False)
            'GoLive()


        End If
    End Sub

    Private Sub Button_Brightfield_Acquire_Click(sender As Object, e As EventArgs) Handles Button_Brightfield_Acquire.Click
        Acquire()
    End Sub

    Public Sub Acquire()
        ExitLive() : Camera.ResetMatrix()
        Thread.Sleep(500)

        Dim bmp As New Bitmap(Camera.CaptureBmp)
        UpdateLED(False)

        SaveFileDialog1.DefaultExt = ".jpg"
        If SaveFileDialog1.ShowDialog() = DialogResult.Cancel Then Exit Sub


        Select Case Display.imagetype
            Case ImagetypeEnum.Brightfield, ImagetypeEnum.Fluorescence

                bmp.Save(SaveFileDialog1.FileName)

                'Dehaze.Apply(bmp).Save("c:\temp\dehazed.bmp")
                'Display.MakeFullsizeImage.Save(SaveFileDialog1.FileName + "_WD.jpg")
                ReDim Preserve Filenames(fileN)
                Filenames(fileN) = SaveFileDialog1.FileName
                fileN += 1
                ListBox1.Items.Add(Path.GetFileName(SaveFileDialog1.FileName))

            Case ImagetypeEnum.EDF_Fluorescence, ImagetypeEnum.EDF_Brightfield
                ReDim Preserve Filenames(fileN)
                bmp = New Bitmap(Camera.W, Camera.H, Imaging.PixelFormat.Format24bppRgb)
                byteToBitmap(ZEDOF.OutputBytes, bmp)
                bmp.Save(SaveFileDialog1.FileName)
                Filenames(fileN) = SaveFileDialog1.FileName
                ListBox1.Items.Add(Path.GetFileName(SaveFileDialog1.FileName))
        End Select


        UpdateLED(CheckBox1.Checked)
        GoLive()
        ' Display.AdjustBrightness()

    End Sub



    Public Function DoAutoFocus(DoInitialize As Boolean, DoRelease As Boolean) As Single
        Stage.GoZero(block)

        Dim WasLive As Boolean
        If Camera.busy Then ExitLive() : WasLive = True


        Dim focus As Single
        If DoInitialize Then AutoFocus.Initialize()
        focus = AutoFocus.Analyze()
        If DoRelease Then AutoFocus.Release()

        'if camera is stopped because  of this sub then it resumes the live.
        If WasLive Then GoLive()

        Return focus
    End Function


    Private Sub Button_Home_Click(sender As Object, e As EventArgs) Handles Button_Home.Click
        Stage.Go_Middle()
    End Sub



    Private Sub TextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.MoveRelative(Stage.Xaxe, TextBox1.Text)
        End If

    End Sub

    Private Sub TextBox2_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox2.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.MoveRelative(Stage.Yaxe, TextBox2.Text)
        End If
    End Sub
    Public Sub SetScan()
        If Slideloaded Then Button_Scan.Enabled = True
        TextBoxX.Text = Tracking.ROIX
        TextBoxY.Text = Tracking.ROIY

    End Sub
    Private Sub Button31_Click(sender As Object, e As EventArgs) Handles Button31.Click
        ExitLive()
        If Scanning Then Scanning = False : Button_Scan.Text = "Scan" : GoLive() : Exit Sub

        SaveFileDialog1.DefaultExt = ".tif"
        If SaveFileDialog1.ShowDialog = DialogResult.Cancel Then GoLive() : Exit Sub
        SaveFileDialog1.AddExtension = True

        Dim Address As String = SaveFileDialog1.FileName
        FastScan2(TextBoxX.Text, TextBoxY.Text, ScanOverlap, Address)
    End Sub


    Private Sub Button_Scan_Click(sender As Object, e As EventArgs) Handles Button_Scan.Click

        FastScan(TextBoxX.Text, TextBoxY.Text, ScanOverlap)

    End Sub


    Public Sub FastScan(X As Integer, Y As Integer, overlap As Integer)

        ExitLive()
        If Scanning Then Scanning = False : Button_Scan.Text = "Scan" : GoLive() : Exit Sub

        SaveFileDialog1.DefaultExt = ".tif"
        If SaveFileDialog1.ShowDialog = DialogResult.Cancel Then GoLive() : Exit Sub
        SaveFileDialog1.AddExtension = True

        Dim Address As String = SaveFileDialog1.FileName

        Dim watch As Stopwatch
        watch = New Stopwatch

        CheckBoxLED.Checked = True
        Scanning = True
        Button_Scan.Text = "Cancel"

        Camera.ResetMatrix()

        'Camera.SetPolicyToSafe()



        Select Case Display.imagetype
            Case ImagetypeEnum.Brightfield
                Camera.SetFlatField("ff.tif", "dark.tif")

            Case ImagetypeEnum.Fluorescence
                Camera.SetFlatField("ff_FiBi.tif", "dark.tif")

        End Select
        Camera.Flatfield(0)
        Dim Hdirection As Integer = 1
        Dim Vdirection As Integer = 1

        ' Creating overlap to enhance the stitching with ICE
        Dim AdjustedStepX As Single = Stage.FOVX * (1 - overlap / Camera.W)
        Dim AdjustedStepY As Single = Stage.FOVY * (1 - overlap / Camera.H)

        Dim cx, cy, cz As Single
        Stage.UpdatePositions()
        cx = Stage.X
        cy = Stage.Y
        cz = Stage.Z

        Pbar.Visible = True
        Pbar.Maximum = X * Y

        Dim Axis As String = ""


        If Tracking.ROI.IsMade Then
            Tracking.MovetoROIEdge()
        End If

        Dim b As Integer = 0




        Dim FileName As String = Path.GetFileNameWithoutExtension(Address)
        Dim Dir As String = Path.Combine(Path.GetDirectoryName(Address), FileName)
        Dim OUTPUT As String = Path.GetDirectoryName(Address) + "\" + FileName + "Stitched.svs"
        Directory.CreateDirectory(Dir)

        Dim Stitcher As New MVStitchintLibrary.StitcherClass
        Dim InputDirectory As New IO.DirectoryInfo(Dir)
        Pbar.Maximum = X * Y
        For b = 0 To ScanBufferSize - 1

            ScanUnits(b).InputSettings(X, Y, Dir, FileName)
        Next
        watch.Start()

        For loop_x = 1 To X
            For loop_y = 1 To Y

                Pbar.Increment(1)
                If Scanning = False Then GoTo 1


                If b = ScanBufferSize Then b = 0
                ScanUnits(b).Acquire(loop_x, loop_y, Hdirection, Vdirection, CheckBox2.Checked)
                b += 1

                If loop_y < Y Then

                    Stage.MoveRelative(Stage.Yaxe, -AdjustedStepY * Hdirection, False)
                    Stage.Y += AdjustedStepY
                    Vdirection = Vdirection * -1

                Else
                    If loop_x < X Then

                        Stage.MoveRelative(Stage.Xaxe, -AdjustedStepX, False)
                        Stage.X += -AdjustedStepX * Hdirection
                        Vdirection = Vdirection * -1
                        Hdirection = Hdirection * -1
                    End If
                End If



                Application.DoEvents()
            Next
        Next


        CheckBoxLED.Checked = False
        Thread.Sleep(2000)
        Pbar.Maximum = 100
        '  Stitcher.Process(Pbar, 2048 - ScanOverlap, 2048 - ScanOverlap, ScanOverlap, InputDirectory, OUTPUT)
1:
        Stage.MoveAbsoluteAsync(Stage.Xaxe, cx)
        Stage.MoveAbsoluteAsync(Stage.Yaxe, cy)
        Stage.MoveAbsoluteAsync(Stage.Zaxe, cz)
        ZEDOF.direction = 1
        Pbar.Value = 0


        watch.Stop()

        MsgBox("Scanned in " + (watch.ElapsedMilliseconds / 1000).ToString + " s")



        ListBox1.Items.Add(Path.GetFileName(SaveFileDialog1.FileName))
        ReDim Preserve Filenames(fileN)
        Filenames(fileN) = SaveFileDialog1.FileName
        fileN += 1
2:

        Scanning = False
        Button_Scan.Text = "Scan"
        GoLive()


    End Sub


    Public Sub MakeMontage(bmp() As Bitmap, x As Integer, y As Integer)

        Dim width As Integer = bmp(0).Width
        Dim height As Integer = bmp(0).Height
        Dim BmpMontage = New Bitmap(x * width, y * height)

        Dim gr As Graphics = Graphics.FromImage(BmpMontage)
        Dim i As Integer

        Pbar.Value = 0
        Application.DoEvents()
        For loop_y = 0 To y - 1
            For loop_x = 0 To x - 1
                gr.DrawImage(bmp(i), loop_x * width, loop_y * height, width, height)
                Pbar.Increment(1)
                Application.DoEvents()
                i = i + 1
            Next
        Next
        Pbar.Value = 0
        BmpMontage.Save("d:\1.png")
    End Sub


    Private Sub TextBox4_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox4.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.MoveAbsolute(Stage.Xaxe, TextBox4.Text)
        End If
    End Sub


    Private Sub TextBox5_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox5.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.MoveAbsolute(Stage.Yaxe, TextBox5.Text)
        End If
    End Sub

    Private Sub TextBox6_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox6.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.MoveAbsolute(Stage.Zaxe, TextBox6.Text)
        End If
    End Sub


    Private Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click
        Stage.CalibrateZoffset()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Tracking.clear()
    End Sub


    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        AcquireFlatFieldOld()
    End Sub

    Public Sub AcquireFlatFieldOld()
        Dim WasLive As Boolean
        If Camera.busy Then ExitLive() : WasLive = True

        Camera.capture()

        Camera.Flatfield(0)
        Camera.SetROI()
        Camera.SetDataMode(Colortype.Grey)
        Camera.SetROI()

        CheckBoxLED.Checked = False
        Thread.Sleep(500)
        Dim dark(Camera.W * Camera.H - 1) As Single

        Dim BLure = New FFTW_VB_Real(Camera.W, Camera.H)
        BLure.MakeGaussianReal(0.1, BLure.MTF, 2)


        'Turning off the color Gains
        'Camera.SetColorGain(1, 1, 1)


        Pbar.Maximum = 100



        For y = 1 To 10
            For x = 1 To 10
                'Stage.MoveRelative(Stage.Xaxe, direction * Stage.FOVX / 10)
                'Camera.capture()
                Pbar.Increment(1)
                For i = 0 To Camera.W * Camera.H - 1

                    dark(i) += Camera.Bytes(i) * 0
                Next
            Next

        Next


        SaveSinglePageTiff16("dark.tif", dark, Camera.W, Camera.H)
        CheckBoxLED.Checked = True
        Thread.Sleep(500)

        Dim Flatfield(Camera.W * Camera.H - 1) As Single
        Dim Flatfieldbytes(Camera.W * Camera.H - 1) As Byte
        Dim direction As Integer = 1
        For y = 1 To 10
            For x = 1 To 10
                'Stage.MoveRelative(Stage.Xaxe, direction * Stage.FOVX / 10)
                Camera.capture()
                Pbar.Increment(1)
                For i = 0 To Camera.W * Camera.H - 1
                    Flatfield(i) += Camera.Bytes(i)
                Next
            Next
            'Stage.MoveRelative(Stage.Yaxe, Stage.FOVY / 10)
            direction *= -1
        Next
        'Stage.MoveRelative(Stage.Yaxe, -5 * Stage.FOVY / 10)
        'Stage.MoveRelative(Stage.Xaxe, -5 * Stage.FOVX / 10)


        'BLure.UpLoad(Flatfield)
        'BLure.Process_FT_MTF()
        'BLure.DownLoad(Flatfield)


        'For i = 0 To Camera.W * Camera.H - 1
        '    'If Flatfield(i) > 255 Then Flatfield(i) = 255
        '    Flatfield(i) = Flatfield(i)
        'Next

        Select Case Display.imagetype
            Case ImagetypeEnum.Brightfield
                SaveSinglePageTiff16("ff.tif", Flatfield, Camera.W, Camera.H)
            Case ImagetypeEnum.Fluorescence
                SaveSinglePageTiff16("ff_FiBi.tif", Flatfield, Camera.W, Camera.H)
            Case ImagetypeEnum.MUSE
                SaveSinglePageTiff16("ff_MUSE.tif", Flatfield, Camera.W, Camera.H)
        End Select



        Camera.SetDataMode(Colortype.RGB)
        Camera.SetROI()



        Select Case Display.imagetype
            Case ImagetypeEnum.Brightfield
                Camera.SetFlatField("ff.tif", "dark.tif")

            Case ImagetypeEnum.Fluorescence
                Camera.SetFlatField("ff_FiBi.tif", "dark.tif")

            Case ImagetypeEnum.MUSE
                Camera.SetFlatField("ff_muse.tif", "dark.tif")
        End Select
        ' setting back the color gain 

        'Display.SetColorGain(Val(TextBox_GainR.Text), Val(TextBox_GainG.Text), Val(TextBox_GainB.Text), Display.imagetype)
        Pbar.Value = 0
        Camera.capture()
        If WasLive Then GoLive()
    End Sub

    Private Sub Button_Acquire_fLUORESCENT_Click(sender As Object, e As EventArgs)
        Acquire()
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        ExitLive()

        Preview.MovetoLoad()
        MsgBox("Load the sample and hit OK.")
        Preview.MovetoPreview()
        GetPreview()
        GoLive()
    End Sub

    Public Sub GetPreview(Optional wait As Boolean = True)


        UpdateLED(False)
        LEDcontroller.SetRelays(PreviewLED, True)
        Tracking.UpdateBmp(Preview.Capture(Val(TextBox_PrevieEXp.Text), Val(TextBox_PreviewFocus.Text)))
        LEDcontroller.SetRelays(PreviewLED, False)
        UpdateLED(CheckBoxLED.Checked)


        'stage.MoveAbsolute(stage.Zaxe, lastZ)
        Dim ID As String = Mid(Now.Year, 3).ToString & Now.Month.ToString & Now.Day.ToString & Now.Hour.ToString & Now.Minute.ToString & Now.Second.ToString
        Tracking.bmp.bmp.Save("C:\Previews\" + ID + ".png")
        Tracking.Pbox.Image = Tracking.bmp.bmp


        Slideloaded = True
        Button_Scan.Enabled = True
        'Stage.MoveAbsolute(Stage.Zaxe, 0)
        Stage.Go_Middle()

        Stage.GoToFocus()
    End Sub
    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        Stage.GoToFocus()
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxLED.CheckedChanged

        UpdateLED(CheckBoxLED.Checked)

    End Sub

    Public Sub UpdateLED(status As Boolean)

        If Display IsNot Nothing Then
            If status Then
                If Display.imagetype = ImagetypeEnum.Brightfield Then

                    LEDcontroller.SetRelays(PreviewLED, False)
                    LEDcontroller.SetRelays(BlueLED, False)
                    LEDcontroller.SetRelays(WhiteLED, True)
                End If

                If Display.imagetype = ImagetypeEnum.Fluorescence Then
                    LEDcontroller.SetRelays(PreviewLED, False)
                    LEDcontroller.SetRelays(BlueLED, True)
                    LEDcontroller.SetRelays(WhiteLED, False)
                End If


            Else
                LEDcontroller.SetRelays(PreviewLED, False)
                LEDcontroller.SetRelays(BlueLED, False)
                LEDcontroller.SetRelays(WhiteLED, False)

            End If
        End If
    End Sub



    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button_GIMP.Click


        Try
            Process.Start("C:\Program Files\GIMP 2\bin\gimp-2.10.exe ", Chr(34) + Filenames(ListBox1.SelectedIndex) + Chr(34))
        Catch
            MsgBox("No image file is found", MsgBoxStyle.Critical)
        End Try


    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click
        Preview.MovetoPreview()
    End Sub

    Private Sub Button13_Click(sender As Object, e As EventArgs) Handles Button13.Click
        Stage.MoveAbsolute(Stage.Zaxe, 0)
        Stage.Go_Middle()
        Stage.GoToFocus()
    End Sub

    Private Sub Button12_Click(sender As Object, e As EventArgs) Handles Button12.Click
        ExitLive()
        UpdateLED(False)
        LEDcontroller.SetRelays(PreviewLED, True)

        Preview.CaptureWhole(Val(TextBox_PrevieEXp.Text), Val(TextBox_PreviewFocus.Text)).Save("c:\temp\whole.jpg")
        Tracking.UpdateBmp(Preview.Capture(Val(TextBox_PrevieEXp.Text), Val(TextBox_PreviewFocus.Text)))
        Preview.Bmp.Save("C:\temp\preview.jpg")
        PictureBox_Preview.Image = Tracking.bmp.bmp
        LEDcontroller.SetRelays(PreviewLED, False)
        UpdateLED(CheckBoxLED.Checked)
        GoLive()
    End Sub


    Private Sub TextBox_PrevieEXp_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_PrevieEXp.KeyDown
        If e.KeyCode = Keys.Return Then
            Setting.Sett("PREVIEWEXP", TextBox_PrevieEXp.Text)
            Preview.SetExposure(TextBox_PrevieEXp.Text)
        End If

    End Sub
    Private Sub TextBox_PreviewFocus_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_PreviewFocus.KeyDown
        If e.KeyCode = Keys.Return Then
            Setting.Sett("PREVIEWFOCUS", TextBox_PreviewFocus.Text)
        End If

    End Sub


    Private Sub TextBox7_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox7.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.SetSpeed(Stage.Xaxe, TextBox7.Text)
        End If
    End Sub



    Private Sub TextBox8_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox8.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.SetSpeed(Stage.Yaxe, TextBox8.Text)
        End If
    End Sub


    Private Sub TextBox9_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox9.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.SetSpeed(Stage.Zaxe, TextBox9.Text)
        End If
    End Sub



    Private Sub TextBox10_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox10.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.SetAcceleration(Stage.Xaxe, TextBox10.Text)
        End If
    End Sub

    Private Sub TextBox11_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox11.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.SetAcceleration(Stage.Yaxe, TextBox11.Text)
        End If
    End Sub


    Private Sub TextBox12_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox12.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.SetAcceleration(Stage.Zaxe, TextBox12.Text)
        End If
    End Sub




    Private Sub Button17_Click(sender As Object, e As EventArgs) Handles Button17.Click

        Dim WasLive As Boolean
        If Camera.busy Then ExitLive() : WasLive = True

        AutoFocus.Calibrate(Pbar)

        'if camera is stopped because  of this sub then it resumes the live.
        If WasLive Then GoLive()

    End Sub




    Private Sub TextBox13_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_exposure.KeyDown
        If e.KeyCode = Keys.Return Then
            ExitLive()
            ChangeExposure()
            GoLive()
        End If
    End Sub







    Private Sub TextBox15_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox15.KeyDown
        If e.KeyCode = Keys.Return Then
            Camera.SetMatrix(TextBox15.Text)
        End If
    End Sub




    Private Sub Button18_Click(sender As Object, e As EventArgs) Handles Button_Luigi.Click
        'Try
        '    Dim viewer As New LuigiViewer.DisplayForm(Filenames(ListBox1.SelectedIndex))
        '    viewer.Show()

        'Catch
        '    MsgBox("No image file is found", MsgBoxStyle.Critical)
        'End Try

    End Sub


    Private Sub Button_Sedeen_Click(sender As Object, e As EventArgs) Handles Button_Sedeen.Click
        Try
            Process.Start("C:\Program Files\Sedeen Viewer\sedeen.exe", Chr(34) + Filenames(ListBox1.SelectedIndex) + Chr(34))
        Catch
            MsgBox("No image file is found", MsgBoxStyle.Critical)
        End Try

    End Sub

    Private Sub Button2_Click_1(sender As Object, e As EventArgs) Handles Button2.Click

        Tracking.MovetoNextDots()

    End Sub



    Private Sub TextBox_FOVX_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_FOVX.KeyDown, TextBox_FOVY.KeyDown
        If e.KeyCode = Keys.Return Then
            Stage.SetFOV(TextBox_FOVX.Text, TextBox_FOVY.Text)
        End If
    End Sub

    Private Sub Button5_Click_1(sender As Object, e As EventArgs) Handles Button5.Click
        If Camera.busy Then ExitLive()
        StopAlign = False
        Dim Montage As New Bitmap(Camera.W * 4, Camera.H * 4)
        Dim BMParray(3) As Bitmap
        For i = 0 To 3
            BMParray(i) = New Bitmap(Camera.W, Camera.H)
        Next

        Dim croppedsize As Integer = TextBox16.Text

        Dim cropped As New Bitmap(croppedsize, croppedsize)
        Dim g As Graphics
        g = Graphics.FromImage(Montage)


        For i = 0 To 3
            If i = 1 Then Stage.MoveRelative(Stage.Xaxe, -Stage.FOVX)
            If i = 2 Then Stage.MoveRelative(Stage.Yaxe, Stage.FOVY)
            If i = 3 Then Stage.MoveRelative(Stage.Xaxe, Stage.FOVX)


            BMParray(i) = New Bitmap(Camera.CaptureBmp)

            If i = 0 Then g.DrawImage(BMParray(i), New Point(0, 0))
            If i = 1 Then g.DrawImage(BMParray(i), New Point(Camera.W, 0))
            If i = 2 Then g.DrawImage(BMParray(i), New Point(Camera.W, Camera.H))
            If i = 3 Then g.DrawImage(BMParray(i), New Point(0, Camera.H))
            Application.DoEvents()

        Next
        Stage.MoveRelative(Stage.Yaxe, -Stage.FOVY)
        cropped = Montage.Clone(New Rectangle(Camera.W - croppedsize / 2, Camera.H - croppedsize / 2, croppedsize, croppedsize), Imaging.PixelFormat.Format24bppRgb)
        PictureBox0.Image = cropped
        cropped.Save("c:\temp\cropped.jpg")
    End Sub

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click
        StopAlign = True
        If Not Camera.busy Then GoLive()
    End Sub




    Private Sub Button18_Click_1(sender As Object, e As EventArgs) Handles Button18.Click
        'TabControl1.SelectedIndex = 0
        'TabControl1.SelectedIndex = 0

        ''EO.Move_A(0)
        'Ximea.exposure = Val(TextBox_exposure.Text)
        'Ximea.SetExposure(Val(TextBox_exposure.Text))
        'ExitLive()
        ''Dim SuperFrmeStack As New Stack
        '' ReDim SuperFrmeStack.bmp(1)
        ''SuperFrmeStack.bmp(0) = New Bitmap(Ximea.bmpRef)

        'Ximea.SetImagingFormat(8)
        'Ximea.SetExposure(Val(TextBox_exposure.Text))
        'Ximea.TRG_MODE = 3
        'Ximea.StartAcquisition()
        'EO.initialDelay = 0
        'EO.setSleep()
        'EO.retrn = True
        'Dim Thread2 As New System.Threading.Thread(AddressOf EO.PiezoScan)
        'Thread2.Start()
        'Ximea.capture()
        'Dim watch As New Stopwatch
        'watch.Start()
        'Ximea.bmpRef = EDF.analyze(Ximea.bmpRef)
        'watch.Stop()
        ''MsgBox(watch.ElapsedMilliseconds)

        ''SuperFrmeStack.bmp(1) = EDF.SuperFrame.bmpRGB
        ''SuperFrmeStack.bmp(1) = New Bitmap(Ximea.bmpRef)
        ''SuperFrmeStack.bmp(3) = New Bitmap(Ximea.bmpRef.Width, Ximea.bmpRef.Height)

        'Ximea.SetImagingFormat(24)
        ''CoolBright()
        'Ximea.StopAcquisition()
        'Muse.Display.imagetype = " <b> DuperFrame </b>, CutOff=  " + EDF.CutOff.ToString
        'SaveFrame()

        ''SuperFrmeStack.MakeMontage(2, 1)
        ''SuperFrmeStack.SaveMontage(2, 1, False)
        '' Ximea.Start()
    End Sub

    Private Sub Button19_Click(sender As Object, e As EventArgs) Handles Button19.Click





        If SaveFileDialog1.ShowDialog = DialogResult.Cancel Then Exit Sub

        ExitLive()

        Camera.SetROI()
        Camera.SetDataMode(Colortype.Grey)
        Camera.SetROI()




        Dim numZ As Integer = 20

        Dim ZupperFrame(numZ - 1)() As Byte

        Dim watch As New Stopwatch

        Pbar.Maximum = 11
        For loop_Z = 0 To numZ - 1
            ReDim ZupperFrame(loop_Z)(Camera.W * Camera.H - 1)
            Camera.capture()

            Buffer.BlockCopy(Camera.Bytes, 0, ZupperFrame(loop_Z), 0, Camera.Bytes.Length)
            Pbar.Increment(1)
            Application.DoEvents()
            Stage.MoveRelative(Stage.Zaxe, 0.002, False)
        Next
        Stage.MoveRelativeAsync(Stage.Zaxe, -0.002 * numZ, False)
        SaveJaggedArray(ZupperFrame, Camera.W, Camera.H, SaveFileDialog1.FileName + ".tif")

        Pbar.Value = 0

        Camera.SetDataMode(Colortype.RGB)
        Camera.SetROI()
        Camera.SetDataMode(Colortype.RGB)

        'Display.BayerInterpolate(EDF.AnalyzeZuper(ZupperFrame), Camera.BmpRef)
        'Camera.BmpRef.Save(SaveFileDialog1.FileName + ".jpg")


        GoLive()
    End Sub



    Private Sub Button14_Click(sender As Object, e As EventArgs) Handles Button14.Click
        Setting.Sett("Xmin", Stage.X)
        Tracking = New TrackingStructure(PictureBox_Preview)
        Tracking.Update()
    End Sub

    Private Sub Button15_Click(sender As Object, e As EventArgs) Handles Button15.Click
        Setting.Sett("ymin", Stage.Y)
        Tracking = New TrackingStructure(PictureBox_Preview)
        Tracking.Update()
    End Sub

    Private Sub Button21_Click(sender As Object, e As EventArgs) Handles Button21.Click
        Setting.Sett("Xmax", Stage.X)
        Tracking = New TrackingStructure(PictureBox_Preview)
        Tracking.Update()
    End Sub

    Private Sub Button20_Click(sender As Object, e As EventArgs) Handles Button20.Click
        Setting.Sett("ymax", Stage.Y)
        Tracking = New TrackingStructure(PictureBox_Preview)
        Tracking.Update()
    End Sub

    Private Sub TextBox18_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox18.KeyDown
        If e.KeyCode = Keys.Return Then
            Piezo.MoveRelative(TextBox18.Text)
        End If
    End Sub


    Private Sub TextBox17_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox17.KeyDown
        If e.KeyCode = Keys.Return Then
            Piezo.MoveAbsolute(TextBox17.Text)
        End If
    End Sub

    Private Sub Button22_Click(sender As Object, e As EventArgs) Handles Button22.Click

        If SaveFileDialog1.ShowDialog = DialogResult.Cancel Then Exit Sub

        ExitLive()
        Camera.StopAcqusition()
        Camera.Flatfield(0)
        Camera.SetDataMode(Colortype.Grey)
        Camera.StartAcqusition()

        Piezo.MoveAbsolute(0)
        Camera.capture()

        Piezo.setSleep(Camera.exp)

        Piezo.MakeDelay()
        Dim Thread2 As New System.Threading.Thread(AddressOf Piezo.Scan)
        Thread2.Start()
        'Piezo.Scan()
        'Camera.Capture_Threaded()
        Camera.capture()

        SaveSinglePageTiff(SaveFileDialog1.FileName + ".tif", Camera.Bytes, Camera.W, Camera.H)
        Piezo.MoveAbsolute(0)

        'Dim BMP As New Bitmap(Camera.W, Camera.H)
        'Display.BayerInterpolate(Camera.Bytes, BMP)
        'PictureBox1.Image = BMP

        Camera.SetDataMode(Colortype.RGB)
        GoLive()

    End Sub

    Private Sub Button23_Click(sender As Object, e As EventArgs) Handles Button23.Click
        ExitLive()
        Camera.StopAcqusition()
        Camera.Flatfield(0)
        Camera.SetDataMode(Colortype.Grey)
        Camera.StartAcqusition()

        Piezo.MoveAbsolute(0)

        Piezo.Scan()
    End Sub

    Private Sub Button24_Click(sender As Object, e As EventArgs) Handles Button24.Click
        Camera.SetDataMode(Colortype.RGB)
        GoLive()
    End Sub

    Private Sub Button25_Click(sender As Object, e As EventArgs) Handles Button25.Click
        Dim WasLive As Boolean
        If Camera.busy Then ExitLive() : WasLive = True

        Piezo.Calibrate(Pbar)

        'if camera is stopped because  of this sub then it resumes the live.
        If WasLive Then GoLive()

    End Sub

    Private Sub CheckBox1_CheckedChanged_1(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If CheckBox1.Checked Then
            LEDcontroller.SetRelays(PreviewLED, True)
        Else
            LEDcontroller.SetRelays(PreviewLED, False)
        End If
    End Sub

    Private Sub Button26_Click(sender As Object, e As EventArgs) Handles Button26.Click
        ExitLive()
        Pbar.Maximum = 200
        For i = 0 To 200
            Camera.CaptureBmp()
            Camera.BmpRef.Save("C:\temp\Laser line generator Triangulation\" + i.ToString + ".jpg")
            Stage.MoveRelative(Stage.Zaxe, 0.001)
            Pbar.Increment(1)
            Application.DoEvents()
        Next
        GoLive()
        Pbar.Value = 0
    End Sub



    Private Sub Button27_Click(sender As Object, e As EventArgs) Handles Button27.Click
        ExitLive()
        Triangle.Initialize()
        Triangle.Capture(TextBox20.Text, TextBox19.Text)
        Triangle.release()
        GoLive()

    End Sub






    Private Sub TextBox21_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox21.KeyDown, TextBox22.KeyDown
        If e.KeyCode = Keys.Return Then
            ZEDOF = New ZstackStructure(Camera.W, Camera.H, TextBox21.Text, TextBox22.Text, TextBox23.Text)
            Setting.Sett("ZSTACRRANGE", TextBox21.Text)
            Setting.Sett("ZSTACKSTEPS", TextBox22.Text)
            Setting.Sett("ZSTACKSCALE", TextBox23.Text)
        End If
    End Sub



    Private Sub Button11_Click(sender As Object, e As EventArgs) Handles Button11.Click



        Select Case Display.imagetype
            Case ImagetypeEnum.Brightfield
                Camera.SetFlatField("ff.tif", "dark.tif")

            Case ImagetypeEnum.Fluorescence
                Camera.SetFlatField("ff_FiBi.tif", "dark.tif")

        End Select

    End Sub

    Private Sub Button32_Click(sender As Object, e As EventArgs) Handles Button32.Click
        Camera.Flatfield(0)
    End Sub


    Private Sub TextBox_PrevieEXp_MouseDown(sender As Object, e As MouseEventArgs) Handles TextBox_PrevieEXp.MouseDown

    End Sub

    Private Sub Button33_Click(sender As Object, e As EventArgs) Handles Button33.Click
        ExitLive()
        'Dim Z As Single

        'For X = -5 To 5 Step 0.1
        UpdateLED(False)
        LEDcontroller.SetRelays(PreviewLED, True)
        Preview.MovetoPreview()
        '  Z = 4.95 + X / 10
        'Preview.EstimateProfile(Z)
        Preview.EstimateProfile()
        LEDcontroller.SetRelays(PreviewLED, False)
        UpdateLED(CheckBoxLED.Checked)
        ' Label25.Text = X
        Application.DoEvents()
        ' Next
        Stage.GoToFocus()
        GoLive()
    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged
        UpdateLED(False)
        LEDcontroller.SetRelays(PreviewLED, CheckBox3.Checked)
    End Sub

    Private Sub RadioButton2_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton_Zprofile.CheckedChanged
        If RadioButton_Zprofile.Checked Then
            PictureBox_Preview.Image = Preview.ZmapBmp.bmp
        Else
            PictureBox_Preview.Image = Preview.Bmp
        End If
    End Sub

    Private Sub PictureBox0_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Button34_Click(sender As Object, e As EventArgs) Handles Button34.Click
        ExitLive() : Camera.ResetMatrix()

        For xx = 1 To 20
            Zprofiler.Acquire(True, 1)

            Zprofiler.EstimateZ()
            Stage.MoveRelative(Stage.Xaxe, -Stage.FOVX)
            saveSinglePage32("c:\temp\" + xx.ToString("D4") + ".tif", Zprofiler.MaxMap2D)
        Next


        GoLive()
    End Sub



    Private Sub PictureBox_Preview_MouseMove(sender As Object, e As MouseEventArgs) Handles PictureBox_Preview.MouseMove
        If RadioButton_Zprofile.Checked Then
            Chart1.Series(0).Points.Clear()
            Chart1.Series(1).Points.Clear()

            Dim x, y As Integer
            Dim min, max As Single
            max = 0
            min = Single.MaxValue

            x = e.X * Tracking.bmp.width / Tracking.Pbox.Width
            y = e.Y * Tracking.bmp.height / Tracking.Pbox.Height

            For zz = 0 To Preview.Z - 1
                If Preview.GreyEdge2D(zz)(y, x) > max Then max = Preview.GreyEdge2D(zz)(y, x)
                If Preview.GreyEdge2D(zz)(y, x) < min Then min = Preview.GreyEdge2D(zz)(y, x)
            Next


            For zz = 0 To Preview.Z - 1
                Chart1.Series(0).Points.AddXY(Preview.Zx(zz), Preview.GreyEdge2D(zz)(y, x) - min)
            Next

            Chart1.Series(1).Points.AddXY(Preview.Zmap(x, y), max - min)
            Label25.Text = Preview.Zmap(x, y)
            Application.DoEvents()
        End If
    End Sub

    Private Sub PictureBox0_MouseDown(sender As Object, e As MouseEventArgs) Handles PictureBox0.MouseDown
        Stage.xp = e.X
        Stage.yp = e.Y

    End Sub

    Private Sub PictureBox0_MouseUp(sender As Object, e As MouseEventArgs) Handles PictureBox0.MouseUp

        Dim Z As Integer = 1
        '   If Display.zoom Then Z = Display.sampeling Else Z = 1

        Stage.MoveRelativeAsync(Stage.Xaxe, (e.X - Stage.xp) * Stage.FOVX * (1 / Z) / PictureBox0.Width)
        Stage.MoveRelativeAsync(Stage.Yaxe, (e.Y - Stage.yp) * Stage.FOVY * (1 / Z) / PictureBox0.Height)
        ExitEDOf()

    End Sub

    Private Sub PictureBox_MouseWheel(sender As Object, e As MouseEventArgs) Handles PictureBox0.MouseWheel
        If Focusing = True Then Exit Sub
        Focusing = True
        ExitEDOf()
        Dim speed As Single
        If System.Windows.Forms.Control.ModifierKeys = Keys.Control Then speed = 20 Else speed = 2

        'If XYZ.name = "NewPort" Then
        If e.Delta > 0 Then
            Stage.MoveRelativeAsync(Stage.Zaxe, speed * 0.001 * Math.Abs(e.Delta) / 120)
        Else
            Stage.MoveRelativeAsync(Stage.Zaxe, speed * -0.001 * Math.Abs(e.Delta) / 120)
        End If
        Focusing = False
    End Sub

    Private Sub PictureBox_Preview_Click(sender As Object, e As EventArgs) Handles PictureBox_Preview.Click

    End Sub

    Private Sub TextBox_exposure_TextChanged(sender As Object, e As EventArgs) Handles TextBox_exposure.TextChanged

    End Sub

    Private Sub TextBox21_TextChanged(sender As Object, e As EventArgs) Handles TextBox21.TextChanged

    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

        If OpenFileDialog1.ShowDialog = DialogResult.Cancel Then Exit Sub
        Dim Directory As String = Path.GetDirectoryName(OpenFileDialog1.FileName)

        Dim Stitcher As New MVStitchintLibrary.StitcherClass
        Dim InputDirectory As New IO.DirectoryInfo(Directory)
        Pbar.Maximum = 100
        Stitcher.Process(Pbar, 2048 - 100, 2048 - 100, 50, InputDirectory, Directory + ".svs")

    End Sub

    Private Sub Button29_Click(sender As Object, e As EventArgs) Handles Button29.Click
        ExitLive()
        For i = 1 To 20
            Camera.capture()
        Next

        GoLive()

    End Sub

    Private Sub Button30_Click(sender As Object, e As EventArgs) Handles Button30.Click
        TextBox_exposure.Text = "----"
        Application.DoEvents()
        ExitLive()
        Thread.Sleep(500)
        SetAutoExposure()
        TextBox_exposure.Text = AutoExposure
        ChangeExposure()
        GoLive()

    End Sub

    Private Sub Button16_Click(sender As Object, e As EventArgs) Handles Button16.Click
        Label25.Text = Preview.GetProfile(Tracking.Cursor.Left, Tracking.Cursor.Top, Tracking.Cursor.Width, Tracking.Cursor.Height)

    End Sub

    Private Sub Button28_Click(sender As Object, e As EventArgs) Handles Button28.Click
        ExitLive()
        Dim Offset As Single

        For X = -0.5 To 0.5 Step 0.1
            UpdateLED(False)
            LEDcontroller.SetRelays(PreviewLED, True)
            Preview.MovetoPreview()
            Offset = X
            Preview.EstimateProfile(Offset)

            LEDcontroller.SetRelays(PreviewLED, False)
            UpdateLED(CheckBoxLED.Checked)
            ' Label25.Text = X
            Application.DoEvents()
        Next
        Stage.GoToFocus()
        GoLive()
    End Sub



    Private Sub TextBoxGY_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBoxGY.KeyDown
        If e.KeyCode = Keys.Enter Then
            Camera.setGammaY(TextBoxGY.Text)
        End If
    End Sub


End Class


