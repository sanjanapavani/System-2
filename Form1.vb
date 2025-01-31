Imports System.ComponentModel
Imports System.IO
Imports System.IO.Ports
Imports AForge.Imaging.Filters
Imports Microsoft.VisualBasic.Devices
Imports AForge.Imaging
Imports System.Threading
Imports OpenSlideSharp
Imports MathNet.Numerics
Imports MVStitchingEnclouser
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.Xml.Linq

Public Class Form1
    Dim IsDragging As Boolean
    Dim Slideloaded As Boolean
    Dim StopAlign As Boolean
    Dim panel As Integer
    Dim Focusing As Boolean
    Dim Filenames() As String
    Dim fileN As Integer
    Dim LivePreview As Boolean = False
    Dim RichardMode As Boolean = True
    Dim RichardScale As Single
    Dim Objective As Objectives
    Dim IamLive As Boolean
    Private svsViewer As SVSViewer
    Private PatientID As String = ""

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadObjects(Pbar, Chart1)

        TextBox21.Text = Setting.Gett("ZSTACRRANGE")
        TextBox22.Text = Setting.Gett("ZSTACKSTEPS")
        TextBox23.Text = Setting.Gett("ZSTACKSCALE")
        InitializeZstack()

        TextBox_PrevieEXp.Text = Setting.Gett("PREVIEWEXP")
        TextBox_PreviewFocus.Text = Setting.Gett("PREVIEWFOCUS")
        Preview.SetExposure(TextBox_PrevieEXp.Text)

        DehazeRadius = Setting.Gett("DEHAZERADIUS")
        DehazeWeight = Setting.Gett("DEHAZEWEIGHT")
        GammaY = Setting.Gett("GAMMAY")
        GammaC = Setting.Gett("GAMMAC")
        Camera.setGammaY(GammaY)
        Camera.setGammaC(GammaC)
        TextBoxGY.Text = GammaY
        TextBoxGC.Text = GammaC

        TextBoxGain.Text = Setting.Gett("Gain")
        TextBox_GainB.Text = Setting.Gett("GainB")
        TextBox_GainG.Text = Setting.Gett("GainG")
        TextBox_GainR.Text = Setting.Gett("GainR")
        TextBox_BlueOffset.Text = Setting.Gett("blueoffset")

        RichardScale = Setting.Gett("Richardscale")
        TextBox_RichardScale.Text = RichardScale
        TextBox_exposure.Text = Setting.Gett("exposure")

        TextBox_FOVX.Text = Stage.FOVX
        TextBox_FOVY.Text = Stage.FOVY

        ReDim FlatFieldB(Camera.W * Camera.H - 1)
        ReDim FlatFieldG(Camera.W * Camera.H - 1)
        ReDim FlatFieldR(Camera.W * Camera.H - 1)

        TabControl1.SelectedIndex = 1
        Display.AcqusitionType = AcqusitionTypes.FiBi

        SetAcqusitionMode()
        ArrangeControls(10)

        ComboBox_Objetives.SelectedIndex = 0
        Preview.Scale = Preview.ROI_W / PictureBox_Preview.Width

        TabControl1.DrawMode = TabDrawMode.OwnerDrawFixed
        TabControl1.Appearance = TabAppearance.Normal
        AddHandler TabControl1.DrawItem, AddressOf TabControl1_DrawItem

        svsViewer = New SVSViewer(PictureBox0)
        svsViewer.Dock = DockStyle.Fill
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        If String.IsNullOrEmpty(PatientID) Then
            PatientID = InputBox("Please enter the Patient ID:", "Patient Identification")
            If String.IsNullOrWhiteSpace(PatientID) Then
                MsgBox("Patient ID is required to proceed.", MsgBoxStyle.Exclamation, "Error")
                Exit Sub
            End If
        End If

        Dim baseFolder As String = "E:\data"
        If Not Directory.Exists(baseFolder) Then
            MsgBox("Error: Data folder not found. Please check settings.", MsgBoxStyle.Critical, "Error")
            Exit Sub
        End If

        Dim patientFolder As String = Path.Combine(baseFolder, PatientID)
        If Not Directory.Exists(patientFolder) Then
            Directory.CreateDirectory(patientFolder)
        End If

        Preview.MovetoLoad()
        MsgBox("Load the sample and hit OK.")
        UpdateLED(False)
        ExitLive()
        Preview.MovetoPreview()
        GetPreview()
        Stage.GoToMiddle()
        GoLive()
    End Sub

    Private Sub Button31_Click(sender As Object, e As EventArgs) Handles Button_Scan2.Click
        Debug.WriteLine("Button31_Click: Method triggered.")

        ' Ensure a Patient ID is entered before scanning
        If String.IsNullOrEmpty(PatientID) Then
            PatientID = InputBox("Please enter the Patient ID:", "Patient Identification")
            If String.IsNullOrWhiteSpace(PatientID) Then
                MsgBox("Patient ID is required to proceed.", MsgBoxStyle.Exclamation, "Error")
                Exit Sub
            End If
        End If

        ' Exit live preview before starting scan
        ExitLive()

        ' Handle scan toggling
        If Scanning Then
            Debug.WriteLine("Button31_Click: Scanning is True, exiting early.")
            Scanning = False
            Button_Scan2.Text = "Scan"
            GoLive()
            Exit Sub
        End If

        ' Confirm scan process
        If MessageBox.Show("The scan process is about to begin. Ensure all settings are correct. Continue?",
                   "Start Scan", MessageBoxButtons.YesNo, MessageBoxIcon.Information) = DialogResult.No Then
            Debug.WriteLine("Button31_Click: User clicked No on the MessageBox.")
            GoLive()
            Exit Sub
        End If

        ' Format timestamp as YYYY-MM-DD_hh-mm
        Dim timestamp As String = DateTime.Now.ToString("yyyy-MM-dd_HH-mm")
        Dim scanFolder As String = Path.Combine("E:\data", PatientID, timestamp & "_" & PatientID)

        ' Ensure directory exists
        If Not Directory.Exists(scanFolder) Then
            Directory.CreateDirectory(scanFolder)
        End If

        MsgBox("Scan folder created: " & scanFolder, MsgBoxStyle.Information, "Scan Ready")

        ' Define file paths with correct format
        Dim svsFilePath As String = Path.Combine(scanFolder, timestamp & "_" & PatientID & ".svs")
        Dim imageFilePath As String = Path.Combine(scanFolder, timestamp & "_" & PatientID & ".png")

        ' Start FastScan process and save the SVS file
        Debug.WriteLine("Button31_Click: Starting FastScan.")
        FastScan(TextBoxX.Text, TextBoxY.Text, ScanOverlap, svsFilePath)

        ' **Fix: Capture and save the image correctly**
        Dim capturedImage As Bitmap = CaptureImage()
        If capturedImage IsNot Nothing Then
            capturedImage.Save(imageFilePath, Imaging.ImageFormat.Png)
            Debug.WriteLine("Image successfully saved: " & imageFilePath)
        Else
            Debug.WriteLine("Error: Image capture failed.")
            MsgBox("Image capture failed. Please try again.", MsgBoxStyle.Exclamation, "Error")
        End If

        ' Log file paths for debugging
        Debug.WriteLine("SVS File Path: " & svsFilePath)
        Debug.WriteLine("Image File Path: " & imageFilePath)
        SaveScanData()
    End Sub

    Private Function CaptureImage() As Bitmap
        Try
            While Camera.busy
                Thread.Sleep(500)
            End While

            If Camera Is Nothing Then
                Return Nothing
            End If

            Camera.Trigger()
            Thread.Sleep(500)

            Dim img As Bitmap = Camera.captureBmp()

            If img IsNot Nothing Then
                Return New Bitmap(img)
            Else
                Return Nothing
            End If
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Sub SaveScanData()
        If String.IsNullOrEmpty(PatientID) Then
            MsgBox("Patient ID is missing. Cannot save scan data.", MsgBoxStyle.Exclamation, "Error")
            Exit Sub
        End If

        Dim timestamp As String = DateTime.Now.ToString("yyyy-MM-dd_HH-mm")
        Dim scanFolder As String = Path.Combine("E:\data", PatientID, timestamp & "_" & PatientID)

        If Not Directory.Exists(scanFolder) Then
            Directory.CreateDirectory(scanFolder)
        End If

        Dim svsFilePath As String = Path.Combine(scanFolder, timestamp & "_" & PatientID & ".svs")
        Dim imageFilePath As String = Path.Combine(scanFolder, timestamp & "_" & PatientID & ".png")

        FastScan(TextBoxX.Text, TextBoxY.Text, ScanOverlap, svsFilePath)

        Dim capturedImage As Bitmap = CaptureImage()
        If capturedImage IsNot Nothing Then
            Try
                capturedImage.Save(imageFilePath, Imaging.ImageFormat.Png)
            Catch ex As Exception
                MsgBox("Error saving image. Check file permissions.", MsgBoxStyle.Critical, "Error")
            End Try
        Else
            MsgBox("Image capture failed. Please try again.", MsgBoxStyle.Exclamation, "Error")
        End If
    End Sub

    Private Sub Button_Done_Click(sender As Object, e As EventArgs) Handles Button_Done.Click
        ExitLive()

        Try
            SaveScanData()

            Stage.MoveAbsolute(Stage.Xaxe, 0)
            Stage.MoveAbsolute(Stage.Yaxe, 0)
            Stage.MoveAbsolute(Stage.Zaxe, 20)
            Thread.Sleep(1000)
            Stage.DisposeCom(DialogResult.OK)

            MessageBox.Show("Done.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
        End Try

        MsgBox("Session ended. To start a new session, click the 'Load' button and enter a new Patient ID.", MsgBoxStyle.Information, "Session Ended")
        PatientID = ""

        GoLive()
    End Sub


    Private Sub TabControl1_DrawItem(sender As Object, e As DrawItemEventArgs)
        Dim g As Graphics = e.Graphics
        Dim tabPage As TabPage = TabControl1.TabPages(e.Index)
        Dim tabBounds As Rectangle = TabControl1.GetTabRect(e.Index)

        If e.State = DrawItemState.Selected Then
            ' Draw a different background color for the selected tab
            g.FillRectangle(New SolidBrush(Color.Gray), tabBounds)
        Else
            ' Draw the default background color for unselected tabs
            g.FillRectangle(New SolidBrush(SystemColors.Control), tabBounds)
        End If

        ' Save the current state of the graphics object
        Dim state As GraphicsState = g.Save()

        ' Set up the string format for centering
        Dim stringFormat As New StringFormat()
        stringFormat.Alignment = StringAlignment.Center
        stringFormat.LineAlignment = StringAlignment.Center

        ' Rotate the text 270 degrees
        g.RotateTransform(270)

        ' Calculate the position to draw the text
        Dim x As Single = -tabBounds.Bottom + (tabBounds.Height / 2)
        Dim y As Single = tabBounds.Left + (tabBounds.Width / 2)

        ' Draw the tab text using DrawString
        g.DrawString(tabPage.Text, TabControl1.Font, Brushes.Black, x, y, stringFormat)

        ' Restore the graphics object to its original state
        g.Restore(state)
    End Sub

    Public Sub InitializeZstack()
        Pbar.Value = 0
        Pbar.Maximum = ScanBufferSize

        'ZEDOF = New ZstackContinuous(Camera.W, Camera.H, TextBox21.Text, TextBox22.Text, TextBox23.Text, DehazeRadius, DehazeWeight, GammaY, Camera)
        For b = 0 To ScanBufferSize - 1
            Pbar.Increment(1)
            ScanUnits(b) = New ZstackContinuous(Camera.W, Camera.H, TextBox21.Text, TextBox22.Text, TextBox23.Text, DehazeRadius, DehazeWeight, GammaY, Camera)

            Application.DoEvents()
        Next b
        Setting.Sett("ZSTACRRANGE", TextBox21.Text)
        Setting.Sett("ZSTACKSTEPS", TextBox22.Text)
        Setting.Sett("ZSTACKSCALE", TextBox23.Text)
        Pbar.Value = 0
        CameraCalibration()
    End Sub
    Sub ArrangeControls(d As Integer)
        Dim scale As Single = 0.45
        'Dim scale As Single = 0.2 * 2708 / 2048
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
        PictureBox_Preview.Width = (TabControl2.Width * 0.3 - 2 * d)

        Tracking = New TrackingStructure(PictureBox_Preview)
        Tracking.Update()

        GroupBox3.Left = PictureBox_Preview.Left + PictureBox_Preview.Width + d
        GroupBox3.Top = PictureBox_Preview.Top

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

        Select Case Display.AcqusitionType
            Case AcqusitionTypes.Slide
                Setting.Sett("EXPOSUREB", Camera.exp)
            Case AcqusitionTypes.FiBi
                Setting.Sett("EXPOSUREF", Camera.exp)
        End Select
        Setting.Sett("EXPOSURE", Camera.exp)
        Camera.ExposureChanged = 0

        'Do Until Camera.ExposureChanged = False

        'Loop
        'Display.AdjustBrightness()
    End Sub



    Public Sub GoLive()


        If (Camera.exp + Camera.readout_time) < 50 Then Timer1.Interval = 50 Else Timer1.Interval = Camera.exp + Camera.readout_time
        'Timer1.Interval = 50
        Camera.busy = False
        Camera.Dostop = False
        Timer1.Start()
        IamLive = True
        'Dim Thread1 As New System.Threading.Thread(AddressOf Live)
        'Thread1.Start()


    End Sub

    Public Sub ExitLive()
        'If Camera.status = False Then Exit Sub
        Timer1.Stop()

        Camera.Dostop = True
        Application.DoEvents()
        IamLive = False

    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick

        CaptureLive()


    End Sub

    Public Sub CaptureLive()

        If Camera.Dostop Then Exit Sub
        If Camera.busy Then Exit Sub

        Camera.busy = True


        If Camera.ExposureChanged = 0 Then Camera.SetExposure() : Camera.capture(False) : Display.AdjustBrightness() : Camera.ExposureChanged = 1
        If Display.RequestIbIc = 0 Then Camera.ResetMatrix() : Display.RequestIbIc = 1
        Camera.capture(False) : Display.MakePreview(Camera.Bytes, True)
        If CheckBoxLED.Checked Then PictureBox0.Image = Display.BmpPreview(Display.f).bmp Else PictureBox0.Image = Display.EmptyPreview.bmp


        Application.DoEvents()
        If Camera.Dostop Then Exit Sub

        Display.MakeHistogram()
        Display.PlotHistogram()
        Application.DoEvents()
        Camera.busy = False

    End Sub
    Private Sub Button_right_Click(sender As Object, e As EventArgs) Handles Button_right.Click

        Stage.MoveRelative(Stage.Xaxe, Stage.FOVX)
        ExitEDOf()
    End Sub

    Private Sub Button_left_Click(sender As Object, e As EventArgs) Handles Button_left.Click

        Stage.MoveRelative(Stage.Xaxe, -Stage.FOVX)
        ExitEDOf()
    End Sub

    Private Sub Button_top_Click(sender As Object, e As EventArgs) Handles Button_top.Click

        Stage.MoveRelative(Stage.Yaxe, -Stage.FOVY)
        ExitEDOf()
    End Sub

    Private Sub Button_bottom_Click(sender As Object, e As EventArgs) Handles Button_bottom.Click

        Stage.MoveRelative(Stage.Yaxe, Stage.FOVY)
        ExitEDOf()
    End Sub

    Private Sub Button_adjustBrightness_Click(sender As Object, e As EventArgs) Handles Button_adjustBrightness.Click

        Display.AdjustBrightness()


    End Sub

    Public Sub ExitEDOf()
        GoLive()
        If Display.AcqusitionType = AcqusitionTypes.EDF_Slide Then TabControl1.SelectedIndex = 0
        If Display.AcqusitionType = AcqusitionTypes.EDF_FiBi Then TabControl1.SelectedIndex = 1
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        ExitLive()

        Stage.MoveAbsolute(Stage.Zaxe, 23)
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


    Private Sub TextBox3_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox3.KeyDown
        If e.KeyCode = Keys.Return Then

            Stage.MoveRelative(Stage.Zaxe, Val(TextBox3.Text))

        End If
    End Sub



    Private Sub TextBox_GainR_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_GainR.KeyDown
        If e.KeyCode = Keys.Return Then
            Try
                Display.SetColorGain(Val(TextBox_GainR.Text), Val(TextBox_GainG.Text), Val(TextBox_GainB.Text), Display.AcqusitionType)
            Catch ex As Exception

            End Try

        End If
    End Sub

    Private Sub TextBox_GainG_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_GainG.KeyDown
        If e.KeyCode = Keys.Return Then
            Try
                Display.SetColorGain(Val(TextBox_GainR.Text), Val(TextBox_GainG.Text), Val(TextBox_GainB.Text), Display.AcqusitionType)
            Catch ex As Exception

            End Try

        End If
    End Sub



    Private Sub TextBox_GainB_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_GainB.KeyDown
        If e.KeyCode = Keys.Return Then
            Try
                Display.SetColorGain(Val(TextBox_GainR.Text), Val(TextBox_GainG.Text), Val(TextBox_GainB.Text), Display.AcqusitionType)
            Catch ex As Exception

            End Try

        End If
    End Sub

    Private Sub TextBoxGain_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBoxGain.KeyDown
        If e.KeyCode = Keys.Return Then
            Camera.setGain(Val(TextBoxGain.Text))
        End If
    End Sub

    Public Sub SetAcqusitionMode()

        '   ExitLive()


        If TabControl1.SelectedIndex = 0 Then
            If Not IamLive Then GoLive()

            Display.AcqusitionType = AcqusitionTypes.Slide
            If Not Display.AcqusitionType = AcqusitionTypes.EDF_Slide Then
                UpdateLED(CheckBoxLED.Checked)
                LoadFlatField(Display.AcqusitionType)
                TextBox_exposure.Text = Setting.Gett("Exposureb")
                TextBox_GainB.Text = Setting.Gett("GainB")
                TextBox_GainG.Text = Setting.Gett("GainG")
                TextBox_GainR.Text = Setting.Gett("GainR")
                TextBox_BlueOffset.Text = 0
                Camera.ResetBlueOffset()
                Display.SetColorGain(Setting.Gett("GainR"), Setting.Gett("GainG"), Setting.Gett("GainB"), AcqusitionTypes.Slide)

                ChangeExposure()
            End If

            ' GoLive()
        End If

        If TabControl1.SelectedIndex = 1 Then
            If Not IamLive Then GoLive()
            Display.AcqusitionType = AcqusitionTypes.FiBi

            If Not Display.AcqusitionType = AcqusitionTypes.EDF_FiBi Then
                UpdateLED(CheckBoxLED.Checked)
                LoadFlatField(Display.AcqusitionType)
                TextBox_exposure.Text = Setting.Gett("Exposuref")
                TextBox_GainB.Text = Setting.Gett("GainB_FiBi")
                TextBox_GainG.Text = Setting.Gett("GainG_FiBi")
                TextBox_GainR.Text = Setting.Gett("GainR_FiBi")
                Display.SetColorGain(Setting.Gett("GainR_FiBi"), Setting.Gett("GainG_FiBi"), Setting.Gett("GainB_FiBi"), AcqusitionTypes.FiBi)
                TextBox_BlueOffset.Text = Setting.Gett("blueoffset")
                Camera.SetBlueOffset(TextBox_BlueOffset.Text)
                ChangeExposure()
            End If


            '  GoLive()
        End If


        If TabControl1.SelectedIndex = 2 Then

            ExitLive()

            If Display.AcqusitionType = AcqusitionTypes.Slide Then
                LoadFlatField(Display.AcqusitionType)
                Display.AcqusitionType = AcqusitionTypes.EDF_Slide

            End If

            If Display.AcqusitionType = AcqusitionTypes.FiBi Then
                ExitRichardMode()
                LoadFlatField(Display.AcqusitionType)
                Display.AcqusitionType = AcqusitionTypes.EDF_FiBi

            End If

            UpdateLED(True)
            Thread.Sleep(5)
            Dim ccMatrix As Single = Camera.CCMAtrix
            Camera.ResetMatrix()




            CrazyCamera = New CrazyCameraClass
            CrazyCamera.StartAcqusition()

            ScanUnits(0).AcquireSmoothX(True, 1)
            'ZEDOF.Acquire(True, 1)

            Do Until ScanUnits(0).WrapUpDone

            Loop



            Dim bmp As New Bitmap(Camera.W, Camera.H, Imaging.PixelFormat.Format24bppRgb)
            byteToBitmap(ScanUnits(0).OutputBytes, bmp)

            'Display.ApplyBrightness(ZEDOF.OutputBytes, ccMatrix, bmp)
            CrazyCamera.StopAcqusition()
            PictureBox0.Image = bmp
            EnterRichardMode()
            UpdateLED(False)

            'GoLive()


        End If

        If TabControl1.SelectedIndex = 3 Then
            ExitLive()
            EnableSVSViewer()
        Else
            DisableSVSViewer()
        End If
    End Sub


    Private Sub EnableSVSViewer()
        ' Logic to enable SVS viewer mode
        ' For example, load an SVS file and display it in SVSViewer
        Dim openFileDialog As New OpenFileDialog()
        openFileDialog.Filter = "SVS files (*.svs)|*.svs|All files (*.*)|*.*"
        If openFileDialog.ShowDialog() = DialogResult.OK Then
            Dim svsFilePath As String = openFileDialog.FileName
            svsViewer.LoadSVSImage(svsFilePath)
        End If
    End Sub

    Private Sub DisableSVSViewer()
        ' Logic to disable SVS viewer mode and revert to normal image display
        PictureBox0.Image = Nothing ' Clear the PictureBox or load a default image
    End Sub

    Private Sub TabControl1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles TabControl1.SelectedIndexChanged
        SetAcqusitionMode()
    End Sub

    Private Sub Button_Brightfield_Acquire_Click(sender As Object, e As EventArgs) Handles Button_Brightfield_Acquire.Click
        Acquire()
    End Sub

    Public Sub Acquire()
        ExitLive() : Camera.ResetMatrix()

        Dim bmpsave As New Bitmap(Camera.W, Camera.H, Imaging.PixelFormat.Format24bppRgb)
        SaveFileDialog1.DefaultExt = ".jpg"



        Select Case Display.AcqusitionType
            Case AcqusitionTypes.Slide, AcqusitionTypes.FiBi

                UpdateLED(True)
                Thread.Sleep(50)
                Camera.capture()
                TurnOffLED()

                If SaveFileDialog1.ShowDialog() = DialogResult.Cancel Then Exit Sub
                'bmp.Save(SaveFileDialog1.FileName)
                'Dehaze.Apply(Camera.Bytes)
                byteToBitmap(Camera.Bytes, bmpsave)
                bmpsave.Save(SaveFileDialog1.FileName)

                'Display.MakeFullsizeImage.Save(SaveFileDialog1.FileName + "_WD.jpg")
                ReDim Preserve Filenames(fileN)
                Filenames(fileN) = SaveFileDialog1.FileName
                fileN += 1
                ListBox1.Items.Add(Path.GetFileName(SaveFileDialog1.FileName))
                GoLive()

            Case AcqusitionTypes.EDF_FiBi, AcqusitionTypes.EDF_Slide
                If SaveFileDialog1.ShowDialog() = DialogResult.Cancel Then Exit Sub
                ReDim Preserve Filenames(fileN)
                Dim bmp = New Bitmap(Camera.W, Camera.H, Imaging.PixelFormat.Format24bppRgb)

                byteToBitmap(ScanUnits(0).OutputBytes, bmp)
                bmp.Save(SaveFileDialog1.FileName)

                'Dehaze.Apply(ZEDOF.OutputBytes)
                'byteToBitmap(ZEDOF.OutputBytes, bmp)
                'bmp.Save(SaveFileDialog1.FileName + "_Dehazed")


                Filenames(fileN) = SaveFileDialog1.FileName

                ListBox1.Items.Add(Path.GetFileName(SaveFileDialog1.FileName))
        End Select




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
        Stage.GoToMiddle()
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
        If Slideloaded Then Button_Scan2.Enabled = True
        TextBoxX.Text = Tracking.ROIX
        TextBoxY.Text = Tracking.ROIY

    End Sub

    ' Private Sub Button31_Click(sender As Object, e As EventArgs) Handles Button_Scan2.Click
    ' Debug.WriteLine("Button31_Click: Method triggered.")

    ' ExitLive()

    'If Scanning Then
    '    Debug.WriteLine("Button31_Click: Scanning is True, exiting early.")
    '   Scanning = False
    '   Button_Scan2.Text = "Scan"
    '  GoLive()
    '  Exit Sub
    ' End If

    ' Debug.WriteLine("Button31_Click: About to show MessageBox.")
    'If 'MessageBox.Show("The scan process is about to begin. Ensure all settings are correct. Continue?",
    '            "Start Scan", MessageBoxButtons.YesNo, MessageBoxIcon.Information) = DialogResult.No Then
    ' Debug.WriteLine("Button31_Click: User clicked No on the MessageBox.")
    ' GoLive()
    'Exit Sub
    '  End If

    '  Debug.WriteLine("Button31_Click: User clicked Yes, proceeding with scan.")
    '   SaveFileDialog1.DefaultExt = ".tif"
    'If SaveFileDialog1.ShowDialog = DialogResult.Cancel Then
    '      Debug.WriteLine("Button31_Click: Save dialog cancelled.")
    '       GoLive()
    'Exit Sub
    'End If
    '   SaveFileDialog1.AddExtension = True

    '   Debug.WriteLine("Button31_Click: Starting FastScan.")
    '   FastScan(TextBoxX.Text, TextBoxY.Text, ScanOverlap, SaveFileDialog1.FileName)
    ' End Sub



    Public Sub FastScan(X As Integer, Y As Integer, overlap As Integer, Address As String)

        ExitLive()

        ' 1/16/25
        ' The next couple of lines is a small hack to shift the Z stage down half the range of the EDOF.
        ' This is done to make sure that the viewing height the user was just at is at the center of the EDOF.
        ' Units of the Z stage are in mm, so we need to convert the range of the EDOF to mm.
        If CheckBox_EDOF.Checked Then
            Dim EDOFShift As Single = (Setting.Gett("ZSTACRRANGE") / 2) / 1000
            Stage.MoveRelative(Stage.Zaxe, -EDOFShift)
        End If

        Scanning = True
        Button_Scan2.Text = "Cancel"
        Camera.ResetMatrix()

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
        Dim OUTPUT As String = Path.GetDirectoryName(Address) + "\" + FileName + ".svs"
        Directory.CreateDirectory(Dir)

        Dim Stitcher As New MVStitchintLibrary.StitcherClass
        Dim InputDirectory As New IO.DirectoryInfo(Dir)
        Pbar.Maximum = X * Y
        For b = 0 To ScanBufferSize - 1
            ScanUnits(b).InputSettings(X, Y, Dir, FileName)
        Next

        If Display.AcqusitionType = AcqusitionTypes.FiBi Or Display.AcqusitionType = AcqusitionTypes.EDF_FiBi Then
            ExitRichardMode()
        End If

        UpdateLED(True)

        ScanUnits(0).SetSpeed(Camera.exp)
        Stage.SetAcceleration(Stage.Zaxe, 500)

        Thread.Sleep(50)

        Dim j As Integer = 0
        Dim loop_n As Integer

        Dim watch As New Stopwatch
        watch.Start()

        CrazyCamera = New CrazyCameraClass
        CrazyCamera.StartAcqusition()

        For loop_x = 1 To X
            For loop_y = 1 To Y

                Pbar.Increment(1)
                If Scanning = False Then GoTo 1

                If b = ScanBufferSize Then b = 0
                If CheckBox_EDOF.Checked Then
                    loop_n += 1
                    ScanUnits(b).AcquireX(loop_x, loop_y, loop_n, Hdirection, Vdirection, False, b)
                Else
                    ScanUnits(b).AcquireSingle(loop_x, loop_y, Hdirection, Vdirection, False, b)
                End If
                b += 1

                Thread.Sleep(100)

                If loop_y < Y Then
                    Stage.MoveRelative(Stage.Yaxe, AdjustedStepY * Hdirection, False, False)
                    Thread.Sleep(30)
                    Stage.Y += AdjustedStepY
                    Vdirection = Vdirection * -1
                Else
                    If loop_x < X Then
                        Stage.MoveRelative(Stage.Xaxe, AdjustedStepX, False, False)
                        Thread.Sleep(30)
                        Stage.X += -AdjustedStepX * Hdirection
                        Vdirection = Vdirection * -1
                        Hdirection = Hdirection * -1
                    End If
                End If

                Application.DoEvents()
            Next
        Next
1:
        watch.Stop()
        CrazyCamera.StopAcqusition()
        EnterRichardMode()
        TurnOffLED()
        CheckBoxLED.Checked = False

        If Scanning = True Then
            Pbar.Value = 0
            MsgBox("Scanned in " + (watch.ElapsedMilliseconds / 1000).ToString + " s")
            Thread.Sleep(5000)
            Pbar.Maximum = 100
            Stitcher.Process(Pbar, 2048 - ScanOverlap, 2048 - ScanOverlap, ScanOverlap / 2, InputDirectory, OUTPUT)
        End If

        Stage.SetSpeed(Stage.Zaxe, Stage.Zspeed)
        Stage.SetAcceleration(Stage.Zaxe, Stage.Zacc)
        Stage.MoveAbsoluteAsync(Stage.Xaxe, cx)
        Stage.MoveAbsoluteAsync(Stage.Yaxe, cy)
        Stage.MoveAbsoluteAsync(Stage.Zaxe, cz)

        Pbar.Value = 0

        ListBox1.Items.Add(Path.GetFileName(SaveFileDialog1.FileName))
        ReDim Preserve Filenames(fileN)
        Filenames(fileN) = SaveFileDialog1.FileName
        fileN += 1
2:
        CheckBoxLED.Checked = False
        Scanning = False
        Button_Scan2.Text = "Scan"

        GoLive()

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
        AcquireFlatfield()
    End Sub
    Public Sub AcquireFlatfield()

        Dim WasLive As Boolean
        If Camera.busy Then ExitLive() : WasLive = True

        Camera.capture()


        If Display.AcqusitionType = AcqusitionTypes.FiBi Then
            ExitRichardMode()
            Camera.ResetBlueOffset()


        End If
        UpdateLED(True)

        Thread.Sleep(500)


        Dim Blure As AForge.Imaging.Filters.GaussianBlur
        Blure = New GaussianBlur(4, 10)

        ReDim FlatField(2)
        ReDim FlatField(0)(Camera.W - 1, Camera.H - 1)
        ReDim FlatField(1)(Camera.W - 1, Camera.H - 1)
        ReDim FlatField(2)(Camera.W - 1, Camera.H - 1)
        Dim f As Integer
        Dim j As Integer = 0
        Dim bmpBlure As FastBMP
        Dim MaxB, MaxG, MaxR As Single
        Dim FlatTemp(Camera.W * Camera.H - 1) As Single
        Pbar.Maximum = 10
        For f = 1 To 10
            'bmpBlure = New FastBMP(Blure.Apply(Camera.captureBmp()))
            bmpBlure = New FastBMP((Camera.captureBmp()))
            j = 0
            For y = 0 To Camera.H - 1
                For x = 0 To Camera.W - 1
                    FlatField(0)(x, y) += bmpBlure.bytes(j)
                    FlatField(1)(x, y) += bmpBlure.bytes(j + 1)
                    FlatField(2)(x, y) += bmpBlure.bytes(j + 2)
                    j += 3

                Next
            Next
            Pbar.Increment(1)
        Next

        '-----------Finding mins-------------------
        Buffer.BlockCopy(FlatField(0), 0, FlatTemp, 0, Camera.W * Camera.H * 4)
        Array.Sort(FlatTemp)
        MaxB = FlatTemp(Camera.W * Camera.H - 50)

        Buffer.BlockCopy(FlatField(1), 0, FlatTemp, 0, Camera.W * Camera.H * 4)
        Array.Sort(FlatTemp)
        MaxG = FlatTemp(Camera.W * Camera.H - 50)

        Buffer.BlockCopy(FlatField(2), 0, FlatTemp, 0, Camera.W * Camera.H * 4)
        Array.Sort(FlatTemp)
        MaxR = FlatTemp(Camera.W * Camera.H - 50)

        '---------nOW INVERTING AND NOMALIZING----------------------

        For y = 0 To Camera.H - 1
            For x = 0 To Camera.W - 1
                FlatField(0)(x, y) = (MaxB / FlatField(0)(x, y))
                FlatField(1)(x, y) = (MaxG / FlatField(1)(x, y))
                FlatField(2)(x, y) = (MaxR / FlatField(2)(x, y))
                j += 3
            Next
        Next


        SaveFlatField(Display.AcqusitionType)
        LoadFlatField(Display.AcqusitionType)
        Pbar.Value = 0
        If Display.AcqusitionType = AcqusitionTypes.FiBi Then

            TextBox_BlueOffset.Text = Setting.Gett("blueoffset")
            Camera.SetBlueOffset(TextBox_BlueOffset.Text)
            EnterRichardMode()
        End If
        TurnOffLED()
        If WasLive Then GoLive()
    End Sub



    Public Sub LoadFlatField(Acqusitiontype As AcqusitionTypes)


        Dim Flatfieldfile As String = ""
        Select Case Objective
            Case Objectives._10X
                If Acqusitiontype = AcqusitionTypes.Slide Or Acqusitiontype = AcqusitionTypes.EDF_Slide Then Flatfieldfile = "ff.tif"
                If Acqusitiontype = AcqusitionTypes.FiBi Or Acqusitiontype = AcqusitionTypes.EDF_FiBi Then Flatfieldfile = "ff_fibi.tif"
            Case Objectives._20X
                If Acqusitiontype = AcqusitionTypes.Slide Or Acqusitiontype = AcqusitionTypes.EDF_Slide Then Flatfieldfile = "ff_20X.tif"
                If Acqusitiontype = AcqusitionTypes.FiBi Or Acqusitiontype = AcqusitionTypes.EDF_FiBi Then Flatfieldfile = "ff_fibi_20X.tif"

        End Select



        Select Case Acqusitiontype
            Case AcqusitionTypes.Slide, AcqusitionTypes.EDF_Slide


                If File.Exists(Flatfieldfile) Then
                    ReadJaggedArray(Flatfieldfile, FlatField)

                    Dim i As Integer = 0
                    For y = 0 To Camera.H - 1
                        For x = 0 To Camera.W - 1
                            FlatFieldG(i) = FlatField(1)(x, y)

                            i += 1
                        Next
                    Next

                End If
            Case AcqusitionTypes.FiBi, AcqusitionTypes.EDF_FiBi
                If File.Exists(Flatfieldfile) Then
                    ReadJaggedArray(Flatfieldfile, FlatField)


                    Dim i As Integer = 0
                    For y = 0 To Camera.H - 1
                        For x = 0 To Camera.W - 1
                            FlatFieldG(i) = FlatField(1)(x, y)
                            i += 1
                        Next
                    Next




                End If

        End Select





    End Sub

    Public Sub SaveFlatField(Acqusitiontype As AcqusitionTypes)


        Select Case Acqusitiontype
            Case AcqusitionTypes.Slide, AcqusitionTypes.EDF_Slide
                If Objective = Objectives._10X Then SaveJaggedArray(FlatField, "ff.tif")
                If Objective = Objectives._20X Then SaveJaggedArray(FlatField, "ff_20X.tif")
            Case AcqusitionTypes.FiBi, AcqusitionTypes.EDF_FiBi
                If Objective = Objectives._10X Then SaveJaggedArray(FlatField, "ff_fibi.tif")
                If Objective = Objectives._20X Then SaveJaggedArray(FlatField, "ff_fibi_20X.tif")
        End Select


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

        Select Case Display.AcqusitionType
            Case AcqusitionTypes.Slide
                SaveSinglePageTiff16("ff.tif", Flatfield, Camera.W, Camera.H)
            Case AcqusitionTypes.FiBi
                SaveSinglePageTiff16("ff_FiBi.tif", Flatfield, Camera.W, Camera.H)

        End Select



        Camera.SetDataMode(Colortype.RGB)
        Camera.SetROI()



        Select Case Display.AcqusitionType
            Case AcqusitionTypes.Slide
                Camera.SetFlatField("ff.tif", "dark.tif")

            Case AcqusitionTypes.FiBi
                Camera.SetFlatField("ff_FiBi.tif", "dark.tif")


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

    ' Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
    '   UpdateLED(False)
    '   ExitLive()
    '   Preview.MovetoLoad()
    '   MsgBox("Load the sample and hit OK.")
    '  Preview.MovetoPreview()
    '  GetPreview()
    '  Stage.GoToMiddle()
    '  GoLive()
    ' End Sub

    Public Sub GetPreview(Optional wait As Boolean = True)



        LEDcontroller.SetRelays(PreviewLED, True)
        Thread.Sleep(500)
        Tracking.UpdateBmp(Preview.Capture(Val(TextBox_PrevieEXp.Text), Val(TextBox_PreviewFocus.Text)))
        LEDcontroller.SetRelays(PreviewLED, False)
        UpdateLED(CheckBoxLED.Checked)


        'stage.MoveAbsolute(stage.Zaxe, lastZ)
        Dim ID As String = Mid(Now.Year, 3).ToString & Now.Month.ToString & Now.Day.ToString & Now.Hour.ToString & Now.Minute.ToString & Now.Second.ToString
        'Tracking.bmp.bmp.Save("C:\Previews\" + ID + ".png")
        Tracking.Pbox.Image = Tracking.bmp.bmp


        Slideloaded = True
        Button_Scan2.Enabled = True
        'Stage.MoveAbsolute(Stage.Zaxe, 0)
        Stage.GoToMiddle()

        Stage.GoToFocus()
    End Sub
    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        Stage.GoToFocus()
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxLED.CheckedChanged

        UpdateLED(CheckBoxLED.Checked)

    End Sub
    Public Sub TurnOnLED()
        If CheckBoxLED.Checked Then UpdateLED(True) Else CheckBoxLED.Checked = True
    End Sub

    Public Sub TurnOffLED()
        If Not CheckBoxLED.Checked Then UpdateLED(False) Else CheckBoxLED.Checked = False
    End Sub
    Public Sub ExitRichardMode()
        Dim exp As Single = Setting.Gett("exposure")
        Camera.SetExposure(exp / RichardScale, False)
        Thread.Sleep(exp)
        RichardMode = False
    End Sub
    Public Sub EnterRichardMode()
        Dim exp As Single = Setting.Gett("exposure")
        Camera.SetExposure(exp, False)
        RichardMode = True
    End Sub

    Public Sub UpdateLED(status As Boolean)

        If Display IsNot Nothing Then
            LEDcontroller.SetRelays(PreviewLED, False)
            LEDcontroller.SetRelays(BlueLED_RichardMode, False)
            LEDcontroller.SetRelays(BlueLED, False)
            LEDcontroller.SetRelays(BacklightWhiteLED, False)

            If Display.AcqusitionType = AcqusitionTypes.Slide Or Display.AcqusitionType = AcqusitionTypes.EDF_Slide Then
                LEDcontroller.SetRelays(BacklightWhiteLED, status)
            End If

            If Display.AcqusitionType = AcqusitionTypes.FiBi Or Display.AcqusitionType = AcqusitionTypes.EDF_FiBi Then

                If RichardMode Then
                    LEDcontroller.SetRelays(BlueLED_RichardMode, status)

                Else
                    LEDcontroller.SetRelays(BlueLED, status)
                End If

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
        Stage.GoToMiddle()
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




    Private Sub Button17_Click(sender As Object, e As EventArgs)

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

    Private Sub Button2_Click_1(sender As Object, e As EventArgs)

        Tracking.MovetoNextDots()

    End Sub



    Private Sub TextBox_FOVX_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_FOVX.KeyDown, TextBox_FOVY.KeyDown
        If e.KeyCode = Keys.Return Then


            Select Case Objective
                Case Objectives._10X
                    Setting.Sett("FOVX_10", TextBox_FOVX.Text)
                    Setting.Sett("FOVY_10", TextBox_FOVY.Text)
                    Stage.SetFOV(TextBox_FOVX.Text, TextBox_FOVY.Text)
                Case Objectives._20X
                    Setting.Sett("FOVX_20", TextBox_FOVX.Text)
                    Setting.Sett("FOVY_20", TextBox_FOVY.Text)
                    Stage.SetFOV(TextBox_FOVX.Text, TextBox_FOVY.Text)
            End Select

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
            If i = 2 Then Stage.MoveRelative(Stage.Yaxe, -Stage.FOVY)
            If i = 3 Then Stage.MoveRelative(Stage.Xaxe, Stage.FOVX)


            BMParray(i) = New Bitmap(Camera.captureBmp)

            If i = 0 Then g.DrawImage(BMParray(i), New Point(0, 0))
            If i = 1 Then g.DrawImage(BMParray(i), New Point(Camera.W, 0))
            If i = 2 Then g.DrawImage(BMParray(i), New Point(Camera.W, Camera.H))
            If i = 3 Then g.DrawImage(BMParray(i), New Point(0, Camera.H))
            Application.DoEvents()

        Next
        Stage.MoveRelative(Stage.Yaxe, Stage.FOVY)
        cropped = Montage.Clone(New Rectangle(Camera.W - croppedsize / 2, Camera.H - croppedsize / 2, croppedsize, croppedsize), Imaging.PixelFormat.Format24bppRgb)
        PictureBox0.Image = cropped
        cropped.Save("c:\temp\cropped.jpg")
    End Sub

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click
        StopAlign = True
        If Not Camera.busy Then GoLive()
    End Sub




    Private Sub Button18_Click_1(sender As Object, e As EventArgs)
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



    Private Sub Button22_Click(sender As Object, e As EventArgs)

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

    Private Sub Button23_Click(sender As Object, e As EventArgs)
        ExitLive()
        Camera.StopAcqusition()
        Camera.Flatfield(0)
        Camera.SetDataMode(Colortype.Grey)
        Camera.StartAcqusition()

        Piezo.MoveAbsolute(0)

        Piezo.Scan()
    End Sub

    Private Sub Button24_Click(sender As Object, e As EventArgs)
        Camera.SetDataMode(Colortype.RGB)
        GoLive()
    End Sub

    Private Sub Button25_Click(sender As Object, e As EventArgs)
        Dim WasLive As Boolean
        If Camera.busy Then ExitLive() : WasLive = True

        Piezo.Calibrate(Pbar)

        'if camera is stopped because  of this sub then it resumes the live.
        If WasLive Then GoLive()

    End Sub


    Private Sub Button26_Click(sender As Object, e As EventArgs)
        ExitLive()
        Pbar.Maximum = 200
        For i = 0 To 200
            Camera.captureBmp()
            Camera.BmpRef.Save("C:\temp\Laser line generator Triangulation\" + i.ToString + ".jpg")
            Stage.MoveRelative(Stage.Zaxe, 0.001)
            Pbar.Increment(1)
            Application.DoEvents()
        Next
        GoLive()
        Pbar.Value = 0
    End Sub










    Private Sub TextBox21_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox21.KeyDown, TextBox22.KeyDown, TextBox23.KeyDown
        If e.KeyCode = Keys.Return Then
            InitializeZstack()
        End If

    End Sub



    Private Sub Button11_Click(sender As Object, e As EventArgs) Handles Button11.Click



        Select Case Display.AcqusitionType
            Case AcqusitionTypes.Slide
                Camera.SetFlatField("ff.tif", "dark.tif")

            Case AcqusitionTypes.FiBi
                Camera.SetFlatField("ff_FiBi.tif", "dark.tif")

        End Select

    End Sub

    Private Sub Button32_Click(sender As Object, e As EventArgs) Handles Button32.Click
        Camera.Flatfield(0)
    End Sub


    Private Sub TextBox_PrevieEXp_MouseDown(sender As Object, e As MouseEventArgs) Handles TextBox_PrevieEXp.MouseDown

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



    Private Sub PictureBox0_MouseDown(sender As Object, e As MouseEventArgs) Handles PictureBox0.MouseDown
        Stage.xp = e.X
        Stage.yp = e.Y

    End Sub

    Private Sub PictureBox0_MouseUp(sender As Object, e As MouseEventArgs) Handles PictureBox0.MouseUp

        Dim Z As Integer = 1
        '   If Display.zoom Then Z = Display.sampeling Else Z = 1

        Stage.MoveRelativeAsync(Stage.Xaxe, -(e.X - Stage.xp) * Stage.FOVX * (1 / Z) / PictureBox0.Width)
        Stage.MoveRelativeAsync(Stage.Yaxe, -(e.Y - Stage.yp) * Stage.FOVY * (1 / Z) / PictureBox0.Height)
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

    Private Sub Button4_Click(sender As Object, e As EventArgs)

        If OpenFileDialog1.ShowDialog = DialogResult.Cancel Then Exit Sub
        Dim Directory As String = Path.GetDirectoryName(OpenFileDialog1.FileName)

        Dim Stitcher As New MVStitchintLibrary.StitcherClass
        Dim InputDirectory As New IO.DirectoryInfo(Directory)
        Pbar.Maximum = 100
        'Stitcher.Process(Pbar, 2048 - 50, 2048 - 50, 50, InputDirectory, Directory + ".svs")
        Stitcher.Process(Pbar, 2048 - ScanOverlap, 2048 - ScanOverlap, ScanOverlap / 2, InputDirectory, Directory + ".svs")

    End Sub


    Private Sub Button30_Click(sender As Object, e As EventArgs) Handles Button30.Click
        TextBox_exposure.Text = "----"
        Application.DoEvents()
        ExitLive()
        'If Camera.isAutoExposure Then
        '    Camera.ToggleAutoExposure(1, 50, 1)
        '    TextBox_exposure.Text = "Auto"
        '    Camera.isAutoExposure = False
        'Else
        '    TextBox_exposure.Text = "Off"
        '    Camera.ToggleAutoExposure(0)
        '    Camera.isAutoExposure = True

        'End If

        Thread.Sleep(500)
        EstimateAutoExposure()
        TextBox_exposure.Text = AutoExposure
        ChangeExposure()
        GoLive()

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



    Private Sub TextBoxGY_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBoxGY.KeyDown, TextBoxGC.KeyDown
        If e.KeyCode = Keys.Enter Then
            Camera.setGammaY(TextBoxGY.Text)
            Camera.setGammaC(TextBoxGC.Text)

            For b = 0 To ScanBufferSize - 1


                'ScanUnits(b).Dehaze.GammaY = TextBoxGY.Text

            Next b

        End If
    End Sub

    Private Sub Button_TurnonGain_Click(sender As Object, e As EventArgs) Handles Button_TurnonGain.Click
        Display.SetColorGain(Val(TextBox_GainR.Text), Val(TextBox_GainG.Text), Val(TextBox_GainB.Text), Display.AcqusitionType)
    End Sub

    Private Sub Button_turnOffGain_Click(sender As Object, e As EventArgs) Handles Button_turnOffGain.Click
        Display.SetColorGain(1, 1, 1, Display.AcqusitionType, False)
    End Sub

    Private Sub Button31_Click_1(sender As Object, e As EventArgs) Handles Button31.Click
        ScanUnits(0).ExportEdge("c:\temp\edges.tif")
        ScanUnits(0).ExportRaw("C:\TEMP\RAWS\")
    End Sub


    Private Sub Button16_Click(sender As Object, e As EventArgs) Handles Button16.Click
        CameraCalibration()


    End Sub
    Public Sub CameraCalibration()
        Dim exp As Single
        Dim ex As Integer

        ExitLive()
        Camera.StopAcqusition()
        'Camera.SetDataMode(Colortype.Grey)
        Camera.StartAcqusition()

        Pbar.Maximum = 500
        Camera.SetExposure(1, False)
        'Camera.SetBurstMode(True, ZEDOF.Z)

        Dim watch As New Stopwatch
        watch.Start()
        'Camera.Trigg()
        Dim testbytes(Camera.W * Camera.H * 3 - 1) As Byte
        For j = 1 To 10
            For i = 1 To ScanUnits(0).Z
                Camera.capture(False)
            Next
        Next
        watch.Stop()

        'Camera.SetBurstMode(False, 50)
        Camera.StopAcqusition()
        'Camera.SetDataMode(Colortype.RGB)
        Camera.StartAcqusition()
        Setting.Sett("CAMERAC", watch.ElapsedMilliseconds / (10 * ScanUnits(0).Z))

        Dim returnExp As Single = Setting.Gett("exposure")
        Camera.SetExposure(returnExp, False)

        MsgBox("Calibration is completed")
        GoLive()
        Pbar.Value = 0

    End Sub
    Private Sub Button29_Click(sender As Object, e As EventArgs) Handles Button29.Click
        Dim exp As Single
        Dim ex As Integer
        ExitLive()

        Dim watch As New Stopwatch
        Pbar.Maximum = 100
        watch.Start()

        For ex = 1 To 100

            Camera.CaptureWithoutTrigg()
        Next



        watch.Stop()
        Pbar.Value = 0
        MsgBox(100 / (watch.ElapsedMilliseconds / 1000))


    End Sub



    Private Sub ComboBox_Objetives_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox_Objetives.SelectedIndexChanged
        Select Case ComboBox_Objetives.SelectedIndex
            Case 0
                Objective = Objectives._10X
                TextBox_FOVX.Text = Setting.Gett("FOVX_10")
                TextBox_FOVY.Text = Setting.Gett("FOVY_10")
                Stage.SetFOV(TextBox_FOVX.Text, TextBox_FOVY.Text)
            Case 1
                Objective = Objectives._20X
                TextBox_FOVX.Text = Setting.Gett("FOVX_20")
                TextBox_FOVY.Text = Setting.Gett("FOVY_20")
                Stage.SetFOV(TextBox_FOVX.Text, TextBox_FOVY.Text)
        End Select
        LoadFlatField(Display.AcqusitionType)
    End Sub

    Private Sub TextBox_FOVX_TextChanged(sender As Object, e As EventArgs) Handles TextBox_FOVX.TextChanged

    End Sub

    Private Sub TextBox25_PreviewKeyDown(sender As Object, e As PreviewKeyDownEventArgs) Handles TextBox_BlueOffset.PreviewKeyDown
        If e.KeyCode = Keys.Enter Then
            Camera.SetBlueOffset(TextBox_BlueOffset.Text)
            Setting.Sett("BLUEOFFSET", TextBox_BlueOffset.Text)
        End If


    End Sub



    Private Sub TextBox_RichardScale_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox_RichardScale.KeyDown
        If e.KeyCode = Keys.Return Then
            RichardScale = TextBox_RichardScale.Text
            Setting.Sett("RichardScale", RichardScale)
        End If
    End Sub

    Private Sub Button19_Click(sender As Object, e As EventArgs)

    End Sub


    Private Sub Button19_Click_1(sender As Object, e As EventArgs)
        UpdateLED(False)
        LEDcontroller.SetRelays(PreviewLED, True)
        Thread.Sleep(50)

        Preview.EstimateProfile(0)
        LEDcontroller.SetRelays(PreviewLED, False)

    End Sub

    Private Sub TextBox_RichardScale_TextChanged(sender As Object, e As EventArgs) Handles TextBox_RichardScale.TextChanged

    End Sub

    Private Sub Button_Reset_Click(sender As Object, e As EventArgs)
        ' Confirm reset action
        Dim result As DialogResult = MessageBox.Show("Are you sure you want to reset all settings?",
                                                     "Confirm Reset",
                                                     MessageBoxButtons.YesNo,
                                                     MessageBoxIcon.Question)
        If result = DialogResult.Yes Then
            ' Reset TextBox values to defaults
            TextBox_exposure.Text = "100"
            TextBox_GainR.Text = "1"
            TextBox_GainG.Text = "1"
            TextBox_GainB.Text = "1"
            TextBox_BlueOffset.Text = "0"
            TextBox_FOVX.Text = "200"
            TextBox_FOVY.Text = "200"

            ' Reset camera settings
            Camera.setGain(1)
            Camera.SetExposure(100, True)
            Camera.SetBlueOffset(0)

            ' Reset stage positions
            Stage.MoveAbsolute(Stage.Xaxe, 0)
            Stage.MoveAbsolute(Stage.Yaxe, 0)
            Stage.MoveAbsolute(Stage.Zaxe, 0)

            ' Reset checkboxes and other controls
            CheckBoxLED.Checked = False
            RadioButton_zoom_in.Checked = False
            RadioButton_Zprofile.Checked = False

            ' Notify the user
            MessageBox.Show("Settings have been reset to default.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

End Class