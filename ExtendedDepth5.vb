Imports System
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Numerics

Imports AForge
Imports AForge.Imaging.Filters

'What is new: I make all of the FFts in VB directly. It is faster better cheaper !
Public Class ExtendedDepth5

    Public Structure SF
        Public bmp As Bitmap
        Public bmpRGB As Bitmap
        Public Bytes() As Single
        Public TwoDArray(,) As Single
        Public BytesLength As Integer
        Public RGBbytesLength As Integer
        Public bytesR() As Single
        Public bytesGL() As Single
        Public bytesGR() As Single
        Public input As inputData
        Public bytesB() As Single
        Public Channel As Colour
        Dim DimX, DimY As Integer
        Dim GRmax, GLmax, Bmax, Rmax As Byte
        Dim GRavg, GLavg, Bavg, Ravg As Single
    End Structure

    Public Structure DF
        Public bmp As Bitmap

        Public Bytes() As Byte
        Public Singles() As Single
        Public LineArray() As Single
        Public TwoDArray(,) As Single
        Public BytesLength As Integer
        Public RGBbytesLength As Integer
        Public bytesR() As Byte
        Public bytesGL() As Byte
        Public bytesGR() As Byte
        Public bytesB() As Byte
        Dim Done As Boolean
        Dim DimX, DimY As Integer
        Dim max, min As Single
        Dim Rflag, GLflag, GRflag, Bflag As Boolean
    End Structure

    Enum Colour
        Red
        GreenL
        GreenR
        Blue
    End Enum

    Enum Position
        TopLeft
        TopRight
        BottomLeft
        BottomRight
    End Enum

    Enum inputData
        bmp
        array
    End Enum

    Enum Qmetrics As Integer
        Good = 1
        bad = 2
        ugly = 4
    End Enum

    Public L(,) As Single
    Public PSF0() As Single
    Public PSF(,,) As Single
    Public PSFZ() As Single
    Public CutOff As Single
    Public OTFz() As Complex
    Public OTF0() As Complex
    Public MTFC() As Complex
    Public MTF() As Single
    Public PSF_Loaded As Boolean
    Public SuperFrame As SF
    Public DuperFrame As DF
    Public Avg As Integer
    Dim Crop As Integer
    Public quality As Qmetrics
    Dim FFTFocus, FFT, FFTB, FFTGR, FFTGL, FFTR, Gblure As FFTW_VB
    Public Captured As Boolean

    Public Sub New(Width As Integer, height As Integer, cutoffin As Single, PSFloaded As Boolean)

        CutOff = cutoffin
        PSF_Loaded = PSFloaded
        '/2 is to take into account the sampling rate of green,blue and red
        SuperFrame.DimX = Width / 2
        SuperFrame.DimY = height / 2


        SuperFrame.input = inputData.array
        ReDim SuperFrame.Bytes(SuperFrame.DimX * SuperFrame.DimY * 2 * 2)
        SuperFrame.bmp = New Bitmap(SuperFrame.DimX * 2, SuperFrame.DimY * 2, PixelFormat.Format8bppIndexed)

        'Initializing DuperFrame
        DuperFrame.bmp = New Bitmap(SuperFrame.DimX * 2, SuperFrame.DimY * 2, PixelFormat.Format8bppIndexed)

        DuperFrame.DimX = DuperFrame.bmp.Width / 2
        DuperFrame.DimY = DuperFrame.bmp.Height / 2

        DuperFrame.BytesLength = DuperFrame.bmp.Width * DuperFrame.bmp.Height - 1
        ReDim DuperFrame.Bytes(DuperFrame.BytesLength)
        DuperFrame.RGBbytesLength = DuperFrame.DimX * DuperFrame.DimY - 1

        ReDim DuperFrame.bytesR(DuperFrame.RGBbytesLength)
        ReDim DuperFrame.bytesGR(DuperFrame.RGBbytesLength)
        ReDim DuperFrame.bytesGL(DuperFrame.RGBbytesLength)
        ReDim DuperFrame.bytesB(DuperFrame.RGBbytesLength)
        ReDim DuperFrame.Singles(SuperFrame.DimX * SuperFrame.DimY - 1)

        'The *2 is to make sure to register real and imaginary components.
        ReDim SuperFrame.bytesR(SuperFrame.DimX * SuperFrame.DimY * 2 - 1)
        ReDim SuperFrame.bytesGR(SuperFrame.DimX * SuperFrame.DimY * 2 - 1)
        ReDim SuperFrame.bytesGL(SuperFrame.DimX * SuperFrame.DimY * 2 - 1)
        ReDim SuperFrame.bytesB(SuperFrame.DimX * SuperFrame.DimY * 2 - 1)

        FFT = New FFTW_VB(SuperFrame.DimX, SuperFrame.DimY, MTF)
        FFTFocus = New FFTW_VB(SuperFrame.DimX, SuperFrame.DimY)

        Dim G(0) As Single
        MakeGaussian(0.005, G)
        GBlure = New FFTW_VB(SuperFrame.DimX, SuperFrame.DimY, G)

        If PSF_Loaded Then
            loadPSF(0.45, 3.8)
        Else
            MakeLukosz(CutOff)
        End If


        loadFFT()
    End Sub

    Public Sub loadFFT()
        FFTB = New FFTW_VB(SuperFrame.DimX, SuperFrame.DimY, MTF)
        FFTGR = New FFTW_VB(SuperFrame.DimX, SuperFrame.DimY, MTF)
        FFTGL = New FFTW_VB(SuperFrame.DimX, SuperFrame.DimY, MTF)
        FFTR = New FFTW_VB(SuperFrame.DimX, SuperFrame.DimY, MTF)
    End Sub

    'creates edf image from byte array
    Public Function analyze(arrayin() As Single) As Byte()
        DuperFrame.Done = False

        Array.Copy(arrayin, SuperFrame.Bytes, arrayin.Length)

        DuperFrame.Rflag = False
        DuperFrame.GRflag = False
        DuperFrame.GLflag = False
        DuperFrame.Bflag = False

        Dim ThreadAnalyzeGR As New System.Threading.Thread(AddressOf AnalyzeGR)
        Dim ThreadAnalyzeGL As New System.Threading.Thread(AddressOf AnalyzeGL)
        Dim ThreadAnalyzeB As New System.Threading.Thread(AddressOf AnalyzeB)
        Dim ThreadAnalyzeR As New System.Threading.Thread(AddressOf AnalyzeR)

        ThreadAnalyzeGR.Start()
        ThreadAnalyzeGL.Start()
        ThreadAnalyzeB.Start()
        ThreadAnalyzeR.Start()

        Do Until (DuperFrame.Rflag And DuperFrame.GRflag And DuperFrame.GLflag And DuperFrame.Bflag)

        Loop

        DuperFrame.Done = True
        Captured = True


        Return DuperFrame.Bytes


    End Function
    Public Function AnalyzeZuper(arrayin()() As Byte) As Byte()
        DuperFrame.Done = False
        Dim b As Integer = 4
        Dim DimZ As Integer = arrayin.GetLength(0)
        Dim SDX(SuperFrame.DimX - 1, SuperFrame.DimY - 1) As Single
        Dim SDY(SuperFrame.DimX - 1, SuperFrame.DimY - 1) As Single
        Dim SD(DimZ - 1)(,) As Single
        ' To accomodate both real and imaginary components
        Dim SDL(SuperFrame.DimX * SuperFrame.DimY * 2 - 1) As Single
        Dim MAxSD(SuperFrame.DimX * 2 - 1, SuperFrame.DimY * 2 - 1) As Integer
        Dim MAxSDSingle(SuperFrame.DimX * 2 - 1, SuperFrame.DimY * 2 - 1) As Single
        Dim max As Single = 0
        ReDim SuperFrame.TwoDArray(SuperFrame.DimX - 1, SuperFrame.DimY - 1)


        Dim Yoffset As Integer = SuperFrame.DimX * 2
        Dim MaxSDBMP As New Bitmap(SuperFrame.DimX * 2, SuperFrame.DimY * 2, PixelFormat.Format8bppIndexed)
        Dim MaxSdbytes(SuperFrame.DimX * 2 * SuperFrame.DimY * 2 - 1) As Byte
        Dim p As Integer
        Dim SuperFrameBMP As New FastBMP(SuperFrame.DimX, SuperFrame.DimY, PixelFormat.Format8bppIndexed)
        Dim Sobel As New SobelEdgeDetector
        Dim homo As New HomogenityEdgeDetector
        Dim diff As New DifferenceEdgeDetector
        Dim cany As New CannyEdgeDetector
        Dim i As Integer = 0
        For z = 0 To DimZ - 1
            ReDim SD(z)(SuperFrame.DimX - 1, SuperFrame.DimY - 1)
            ' reading the array
            p = Yoffset
            i = 0
            For Y = 0 To SuperFrame.DimY - 1
                For X = 0 To SuperFrame.DimX - 1
                    SuperFrame.TwoDArray(X, Y) = arrayin(z)(p)
                    SuperFrameBMP.bytes(i) = arrayin(z)(p)
                    p += 2
                    i += 1
                Next
                p = p + Yoffset
            Next
            SuperFrameBMP.MakeNewFromBytes()
            Sobel.ApplyInPlace(SuperFrameBMP.bmp)
            BitmapToBytes(SuperFrameBMP.bmp, SuperFrameBMP.bytes)

            i = 0
            For Y = 0 To SuperFrame.DimY - 1
                For X = 0 To SuperFrame.DimX - 1
                    SD(z)(X, Y) = SuperFrameBMP.bytes(i)
                    i += 1
                Next
            Next


            SD(z) = GBlure.DFT2D_MTF(SD(z))

        Next

        For y = 0 To SuperFrame.DimY - 1
            For x = 0 To SuperFrame.DimX - 1
                max = 0
                For z = 0 To DimZ - 1
                    If SD(z)(x, y) > max Then max = SD(z)(x, y)
                Next
                ' to account for the RGGB bayer pattern
                For z = 0 To DimZ - 1
                    If SD(z)(x, y) = max Then
                        MAxSD(x * 2, y * 2) = z
                        MAxSD(x * 2 + 1, y * 2 + 1) = z
                        MAxSD(x * 2 + 1, y * 2) = z
                        MAxSD(x * 2, y * 2 + 1) = z
                    End If
                Next
            Next
        Next



        p = 0
        For y = 0 To SuperFrame.bmp.Height - 1
            For x = 0 To SuperFrame.bmp.Width - 1
                DuperFrame.Bytes(p) = arrayin(MAxSD(x, y))(p)
                p += 1
            Next
        Next

        DuperFrame.Done = True
        Captured = True


        Return DuperFrame.Bytes
    End Function
    Public Sub GetSingleBytes(bmpin As Bitmap, ByRef ArrayOut As Single())

        Dim BytesIn(bmpin.Width * bmpin.Height * 3 - 1) As Byte
        ReDim ArrayOut(bmpin.Width * bmpin.Height - 1)
        BitmapToBytes(bmpin, BytesIn)

        Dim Pin, Pout As Integer

        For i = 0 To bmpin.Width - 1
            For j = 0 To bmpin.Height - 1

                ArrayOut(Pout) = BytesIn(Pin)
                Pin += 3
                Pout += 1
            Next
        Next



    End Sub

    Public Function Analyz(bmpin As Bitmap) As Bitmap



        DuperFrame.Done = False


        GetSingleBytes(bmpin, SuperFrame.Bytes)



        DuperFrame.Rflag = False
        DuperFrame.GRflag = False
        DuperFrame.GLflag = False
        DuperFrame.Bflag = False

        Dim ThreadAnalyzeGR As New System.Threading.Thread(AddressOf AnalyzeGR)
        Dim ThreadAnalyzeGL As New System.Threading.Thread(AddressOf AnalyzeGL)
        Dim ThreadAnalyzeB As New System.Threading.Thread(AddressOf AnalyzeB)
        Dim ThreadAnalyzeR As New System.Threading.Thread(AddressOf AnalyzeR)

        ThreadAnalyzeGR.Start()
        ThreadAnalyzeGL.Start()
        ThreadAnalyzeB.Start()
        ThreadAnalyzeR.Start()

        Do Until (DuperFrame.Rflag And DuperFrame.GRflag And DuperFrame.GLflag And DuperFrame.Bflag)

        Loop

        DuperFrame.Done = True



        byteToBitmap(DuperFrame.Bytes, DuperFrame.bmp)


        Dim BayerFilter As BayerFilterOptimized = New BayerFilterOptimized()
        Dim Sum As Integer
        For i = 0 To DuperFrame.Bytes.Length - 1
            Sum += DuperFrame.Bytes(i)
        Next
        DuperFrame.bmp = (BayerFilter.Apply(DuperFrame.bmp))

        'Crop = 15
        'Dim Cropfilter As Crop = New Crop(New Rectangle(Crop, Crop, DuperFrame.bmp.Width - 2 * Crop, DuperFrame.bmp.Height - 2 * Crop))

        'DuperFrame.bmp = Cropfilter.Apply(DuperFrame.bmp)
        Return (DuperFrame.bmp)
    End Function

    Public Function FindCenterOfMass(arrayin() As Byte) As Single
        'I will only use the green to estimate the focus
        arrayin.CopyTo(SuperFrame.Bytes, 0)

        GetSuperFrameColorBytes(SuperFrame.bytesGL, Position.BottomLeft)

        DuperFrame.Singles = FFTFocus.DFT2DM(SuperFrame.bytesGL)
        'sends only the middle  line of the transformation.
        'Array.Copy(DuperFrame.Singles, SuperFrame.DimX * SuperFrame.DimY \ 2, DuperFrame.LineArray, 0, SuperFrame.DimX)
        Dim p As Integer


        ReDim DuperFrame.TwoDArray(SuperFrame.DimX - 1, SuperFrame.DimY - 1)

        ReDim DuperFrame.LineArray(SuperFrame.DimX - 1)

        '  Buffer.BlockCopy(DuperFrame.Singles, 0, DuperFrame.TwoDArray, 0, SuperFrame.DimX * SuperFrame.DimY * 4 - 1)

        For y = 0 To DuperFrame.DimY - 1
            For x = 0 To DuperFrame.DimX - 1
                DuperFrame.TwoDArray(x, y) = DuperFrame.Singles(p)
                p += 1
            Next
        Next
        p = 0


        ' since the data os real, only half of the FFT is sufficient
        'saveSinglePage32("C:\temp\ff.tif", DuperFrame.TwoDArray)
        Dim r As Integer
        For y = 0 To SuperFrame.DimY / 2 - 1
            For x = 0 To SuperFrame.DimX / 2 - 1
                r = Math.Sqrt(x ^ 2 + y ^ 2)
                DuperFrame.LineArray(r) += (DuperFrame.TwoDArray(x, y))
                '     p += 1
            Next
        Next

        'center of Mass
        Dim Cm As Single = 0
        Dim sum As Single = DuperFrame.LineArray.Sum

        For i = 0 To SuperFrame.DimX / 4 - 1
            DuperFrame.LineArray(i) = DuperFrame.LineArray(i) / sum
        Next



        For i = 0 To SuperFrame.DimX / 4 - 1
            Cm += DuperFrame.LineArray(i) * i
        Next
        Return Cm

    End Function

    Public Function FindCenterOfMass2(arrayin() As Byte, ByRef LINEARRAYOUT() As Single) As Single
        'I will only use the green to estimate the focus
        arrayin.CopyTo(SuperFrame.Bytes, 0)

        GetSuperFrameColorBytes(SuperFrame.bytesGL, Position.BottomLeft)

        DuperFrame.Singles = FFTFocus.DFT2DM(SuperFrame.bytesGL)
        'sends only the middle  line of the transformation.
        'Array.Copy(DuperFrame.Singles, SuperFrame.DimX * SuperFrame.DimY \ 2, DuperFrame.LineArray, 0, SuperFrame.DimX)
        Dim p As Integer


        ReDim DuperFrame.TwoDArray(SuperFrame.DimX - 1, SuperFrame.DimY - 1)

        ReDim DuperFrame.LineArray(SuperFrame.DimX - 1)
        ReDim LINEARRAYOUT(SuperFrame.DimX - 1)
        '  Buffer.BlockCopy(DuperFrame.Singles, 0, DuperFrame.TwoDArray, 0, SuperFrame.DimX * SuperFrame.DimY * 4 - 1)

        For y = 0 To DuperFrame.DimY - 1
            For x = 0 To DuperFrame.DimX - 1
                DuperFrame.TwoDArray(x, y) = DuperFrame.Singles(p)
                p += 1
            Next
        Next
        p = 0


        ' since the data os real, only half of the FFT is sufficient
        'saveSinglePage32("C:\temp\ff.tif", DuperFrame.TwoDArray)
        Dim r As Integer
        Dim nr(SuperFrame.DimX) As Integer
        For y = 0 To SuperFrame.DimY / 2 - 1
            For x = 0 To SuperFrame.DimY / 2 - 1
                r = Math.Sqrt(x ^ 2 + y ^ 2)
                DuperFrame.LineArray(r) += (DuperFrame.TwoDArray(x, y))
                nr(r) += 1
                '     p += 1
            Next
        Next

        'This is to normlize to the number of pixels traken.
        'For r = 0 To SuperFrame.DimY / 2 - 1
        '    DuperFrame.LineArray(r) = (DuperFrame.LineArray(r) / nr(r))
        'Next


        'center of Mass
        Dim Cm As Single = 0
        ' I have removed total normalization, as usually, focus is also brighter.
        Dim sum As Single = DuperFrame.LineArray.Sum
        For i = 0 To SuperFrame.DimY / 2
            Cm += (DuperFrame.LineArray(i))
        Next
        LINEARRAYOUT = DuperFrame.LineArray
        Return Cm

    End Function
    Public Function FindCenterOfMass3(arrayin() As Byte) As Single
        'I will only use the green to estimate the focus
        arrayin.CopyTo(SuperFrame.Bytes, 0)

        GetSuperFrameColorBytes(SuperFrame.bytesGL, Position.BottomLeft)

        DuperFrame.Singles = FFTFocus.DFT2DM(SuperFrame.bytesGL)
        'sends only the middle  line of the transformation.
        'Array.Copy(DuperFrame.Singles, SuperFrame.DimX * SuperFrame.DimY \ 2, DuperFrame.LineArray, 0, SuperFrame.DimX)
        Dim p As Integer


        ReDim DuperFrame.TwoDArray(SuperFrame.DimX - 1, SuperFrame.DimY - 1)

        ReDim DuperFrame.LineArray(SuperFrame.DimX - 1)

        '  Buffer.BlockCopy(DuperFrame.Singles, 0, DuperFrame.TwoDArray, 0, SuperFrame.DimX * SuperFrame.DimY * 4 - 1)

        For y = 0 To DuperFrame.DimY - 1
            For x = 0 To DuperFrame.DimX - 1
                DuperFrame.TwoDArray(x, y) = DuperFrame.Singles(p)
                p += 1
            Next
        Next
        p = 0


        ' since the data os real, only half of the FFT is sufficient
        'saveSinglePage32("C:\temp\ff.tif", DuperFrame.TwoDArray)
        Dim Mx(SuperFrame.DimY / 3 - 1) As Single
        Dim Mxx As Single
        For y = 0 To SuperFrame.DimY / 3 - 1
            For x = 0 To SuperFrame.DimX / 3 - 1
                Mx(y) += x * DuperFrame.TwoDArray(x, y)
            Next
        Next
        Mxx = Mx.Sum / DuperFrame.TwoDArray(0, 0)

        Dim My(SuperFrame.DimX / 3 - 1) As Single
        Dim Myy As Single

        For x = 0 To SuperFrame.DimX / 3 - 1
            For y = 0 To SuperFrame.DimY / 3 - 1
                My(x) += y * DuperFrame.TwoDArray(x, y)
            Next
        Next
        Myy = My.Sum / DuperFrame.TwoDArray(0, 0)


        Return (Mxx ^ 2 + Myy ^ 2)

    End Function
    Private Sub AnalyzeB()
        GetSuperFrameColorBytes(SuperFrame.bytesB, Position.BottomRight)
        'DuperFrame.bytesB = DeconvolveVB(SuperFrame.bytesB)
        DuperFrame.bytesB = FFTB.DFT2D_MTF(SuperFrame.bytesB)
        SetDuperFrameColorBytes(DuperFrame.bytesB, Position.BottomRight)
        DuperFrame.Bflag = True

    End Sub

    Private Sub AnalyzeGR()
        GetSuperFrameColorBytes(SuperFrame.bytesGR, Position.TopRight)
        'DuperFrame.bytesGR = DeconvolveVB(SuperFrame.bytesGR)

        DuperFrame.bytesGR = FFTGR.DFT2D_MTF(SuperFrame.bytesGR)

        SetDuperFrameColorBytes(DuperFrame.bytesGR, Position.TopRight)
        DuperFrame.GRflag = True
    End Sub

    Private Sub AnalyzeGL()
        GetSuperFrameColorBytes(SuperFrame.bytesGL, Position.BottomLeft)
        'DuperFrame.bytesGL = DeconvolveVB(SuperFrame.bytesGL)

        DuperFrame.bytesGL = FFTGL.DFT2D_MTF(SuperFrame.bytesGL)
        SetDuperFrameColorBytes(DuperFrame.bytesGL, Position.BottomLeft)
        DuperFrame.GLflag = True
    End Sub


    Private Sub AnalyzeR()
        GetSuperFrameColorBytes(SuperFrame.bytesR, Position.TopLeft)
        'DuperFrame.bytesR = DeconvolveVB(SuperFrame.bytesR)
        DuperFrame.bytesR = FFTR.DFT2D_MTF(SuperFrame.bytesR)
        SetDuperFrameColorBytes(DuperFrame.bytesR, Position.TopLeft)
        DuperFrame.Rflag = True
    End Sub

    Public Sub GetSuperFrameBytes()


        SuperFrame.BytesLength = SuperFrame.bmp.Width * SuperFrame.bmp.Height - 1
        ReDim SuperFrame.Bytes(SuperFrame.BytesLength)


        Dim rect As Rectangle = New Rectangle(0, 0, SuperFrame.DimX, SuperFrame.DimY)
        Dim bmpData As BitmapData = SuperFrame.bmp.LockBits(rect, ImageLockMode.ReadWrite, SuperFrame.bmp.PixelFormat)
        Dim bytes(SuperFrame.BytesLength) As Byte

        If SuperFrame.bmp.PixelFormat = PixelFormat.Format8bppIndexed Then
            System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, bytes, 0, SuperFrame.Bytes.Length)
            'converting the byte to single 
            For j = 0 To SuperFrame.BytesLength : SuperFrame.Bytes(j) = bytes(j) : Next

        Else
            Dim SuperframeBytes32(SuperFrame.bmp.Width * SuperFrame.bmp.Height * 4 - 1) As Byte
            System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, SuperframeBytes32, 0, SuperframeBytes32.Length)
            Dim len As Integer = SuperframeBytes32.Length - 1

            Dim i As Integer = 0
            For p = 0 To len - 1 Step 4
                SuperFrame.Bytes(i) = SuperframeBytes32(p)
                i += 1
            Next

        End If

        'Muse.Super = EDF.SuperFrame.bmp.Clone()
        SuperFrame.bmp.UnlockBits(bmpData)

    End Sub

    Public Sub GetSuperFrameColorBytes(ByRef bytes() As Single, channel As Position)
        Dim Yoffset As Integer = SuperFrame.DimX * 2
        Dim i As Integer
        Dim p As Integer

        Select Case channel
            Case Position.TopLeft
                p = 0
            Case Position.TopRight
                p = 1
            Case Position.BottomLeft
                p = Yoffset
            Case Position.BottomRight
                p = Yoffset + 1
        End Select

        For Y = 0 To SuperFrame.DimY - 1
            For X = 0 To SuperFrame.DimX - 1

                bytes(i) = SuperFrame.Bytes(p)
                '  bytes(i) = 50
                p = p + 2
                i = i + 2
            Next
            p = p + Yoffset
        Next


    End Sub

    Public Sub SetDuperFrameColorBytes(bytes() As Byte, channel As Position)
        Dim Yoffset As Integer = SuperFrame.DimX * 2
        Dim i As Integer
        Dim p As Integer

        Select Case channel
            Case Position.TopLeft
                p = 0
            Case Position.TopRight
                p = 1
            Case Position.BottomLeft
                p = Yoffset
            Case Position.BottomRight
                p = Yoffset + 1
        End Select

        For Y = 0 To SuperFrame.DimY - 1
            For X = 0 To SuperFrame.DimX - 1

                DuperFrame.Bytes(p) = bytes(i)

                p = p + 2
                i = i + 1
            Next
            p = p + Yoffset
        Next


    End Sub

    'windowing method for leaving out noise
    Public Sub MakeLukosz(qc As Single)
        Dim n As Single
        qc = qc * SuperFrame.DimX


        ReDim L(SuperFrame.DimX, SuperFrame.DimY)
        For x = 0 To SuperFrame.DimX - 1
            For y = 0 To SuperFrame.DimY - 1

                n = Math.Sqrt(((x - SuperFrame.DimX / 2) * (SuperFrame.DimY / SuperFrame.DimX)) ^ 2 + (y - SuperFrame.DimY / 2) ^ 2)
                ' If (n <= qc) Then
                L(x, y) = Math.Min(Math.Cos((Math.PI * n) / (n + qc)), (Math.Cos((Math.PI * n) / (n + Math.Sqrt(2) * qc))) ^ 2)
                If L(x, y) < 0 Then L(x, y) = 0

                'End If

            Next
        Next

        'Dim LL(SuperFrame.DimX, SuperFrame.DimY) As Byte
        'Dim bmp As New Bitmap(SuperFrame.DimX, SuperFrame.DimY)
        'For x = 0 To SuperFrame.DimX - 1
        '    For y = 0 To SuperFrame.DimY - 1
        '        LL(x, y) = L(x, y) * 255
        '        bmp.SetPixel(x, y, Color.FromArgb(255, LL(x, y), LL(x, y), LL(x, y)))
        '    Next
        'Next
        'bmp.Save("C:\1.png")

        'L = fftshift(L)


        ReDim MTFC(SuperFrame.DimX * SuperFrame.DimY - 1)
        ReDim MTF(SuperFrame.DimX * SuperFrame.DimY * 2 - 1)

        Dim p As Integer = 0

        If PSF_Loaded Then

            For j = 0 To SuperFrame.DimY - 1
                For i = 0 To SuperFrame.DimX - 1
                    '(SuperFrame.DimX * SuperFrame.DimY) ^ 2 This is to take  into account the required normalization for FFT directions
                    MTFC(p) = (1 / OTFz(p) * L(i, j)) / (SuperFrame.DimX * SuperFrame.DimY) ^ 2

                    MTF(2 * p) = MTFC(p).Real
                    MTF(2 * p + 1) = MTFC(p).Imaginary
                    p = p + 1
                Next
            Next

        Else

            For j = 0 To SuperFrame.DimY - 1
                For i = 0 To SuperFrame.DimX - 1
                    '(SuperFrame.DimX * SuperFrame.DimY) ^ 2 This is to take  into account the required normalization for FFT directions

                    MTF(2 * p) = L(i, j) / (SuperFrame.DimX * SuperFrame.DimY)
                    MTF(2 * p + 1) = L(i, j) / (SuperFrame.DimX * SuperFrame.DimY)
                    p = p + 1
                Next
            Next

        End If


    End Sub


    Private Function fftshift(img(,) As Single) As Single(,)
        Dim DimX As Integer = img.GetLength(0)
        Dim DimY As Integer = img.GetLength(1)

        Dim tmp13 As Single, tmp24 As Single
        Dim DimX2, DimY2 As Integer
        DimX2 = DimX / 2 - 1
        ' half of row dimension
        DimY2 = DimY / 2 - 1
        ' half of column dimension
        ' interchange entries in 4 quadrants, 1 <--> 3 and 2 <--> 4

        For x = 0 To DimX2
            For y = 0 To DimY2
                tmp13 = img(x, y)
                img(x, y) = img(x + DimX2, y + DimY2)
                img(x + DimX2, y + DimY2) = tmp13

                tmp24 = img(x + DimX2, y)
                img(x + DimX2, y) = img(x, y + DimY2)
                img(x, y + DimY2) = tmp24
            Next
        Next
        Return img
    End Function

    Public Sub byteToBitmap(bytes() As Byte, bmp As Bitmap)

        Dim rect As Rectangle = New Rectangle(0, 0, bmp.Width, bmp.Height)
        Dim bmpData As BitmapData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat)

        ' Copy the RGB values back to the bitmap
        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, bmpData.Scan0, bytes.Length)
        ' Unlock the bits.
        bmp.UnlockBits(bmpData)


    End Sub

    'makes a new psf
    Public Sub MakePSF(s0 As Single, mz As Single, Z As Integer)
        Dim DimX As Integer = SuperFrame.DimX
        Dim DimY As Integer = SuperFrame.DimY

        Dim p As Integer = 0


        ReDim PSF(DimX - 1, DimY - 1, 2 * Z)
        ReDim PSFZ(DimX * DimY * 2 - 1)
        ReDim PSF0(DimX * DimY * 2 - 1)

        For Y = 0 To DimY - 1
            For X = 0 To DimX - 1
                For zz = -Z To Z
                    PSF(X, Y, zz + Z) = (1 / (Math.Abs(mz * zz) + s0) ^ 2) * Math.Exp(-((X - DimX / 2) ^ 2 + (Y - DimY / 2) ^ 2) / (2 * (Math.Abs(mz * zz) + s0) ^ 2)) * 4096
                Next

            Next
        Next

        Dim z0 As Integer = Z

        ReDim PSFZ(DimX * DimY * 2 - 1)
        ReDim PSF0(DimX * DimY * 2 - 1)

        For Y = 0 To DimY - 1
            For X = 0 To DimX - 1
                For zz = 0 To Z
                    PSFZ(p) += PSF(X, Y, zz)
                Next

                'PSF0(p) = PSF(X, Y, Z0)
                If X = DimX / 2 And Y = DimY / 2 Then PSF0(p) = 1
                p = p + 2
            Next
        Next

        p = 0

        Dim PSFZSUM As Single = PSFZ.Sum
        Dim PSF0SUM As Single = PSF0.Sum

        For Y = 0 To DimY - 1
            For X = 0 To DimX - 1
                PSFZ(p) = PSFZ(p) / PSFZSUM

                PSF0(p) = PSF0(p) / PSF0SUM
                p = p + 2
            Next
        Next

    End Sub


    Public Sub MakeOTF()
        Dim p As Integer = 0

        Dim DimX As Integer = SuperFrame.DimX
        Dim DimY As Integer = SuperFrame.DimY
        Dim Z0 As Integer = (PSF.GetLength(2) - 1) / 2
        Dim Z As Integer = PSF.GetLength(2) - 1

        'Dim page As Integer = Val(Form1.TextBox1.Text)



        OTFz = FFT.DFT2DC(PSFZ, DimX, DimY)
        OTF0 = FFT.DFT2DC(PSF0, DimX, DimY)


        p = 0
        For Y = 0 To DimY - 1
            For X = 0 To DimX - 1
                ' If OTF0(p).Magnitude = 0 Then OTF0(p) = 1

                If OTFz(p).Magnitude = 0 Then OTFz(p) = 1
                'This is for the normalization process, then you don't have to do it during the deconvolution
                OTFz(p) = (OTFz(p) / OTF0(p)) / (DimX * DimY)
                p = p + 1

            Next
        Next

        ' Save_MultiTiff()
    End Sub


    'Public Sub loadPSF(address As String)
    '    PSF = ReadMultiPage(address)
    '    MakeOTF()
    '    MakeLukosz(CutOff)
    '    PSF_Loaded = True
    '    DuperFrame.Done = True
    'End Sub

    Public Sub loadPSF(s0 As Single, mz As Single)
        MakePSF(s0, mz, 20)
        MakeOTF()
        MakeLukosz(CutOff)
        PSF_Loaded = True
        DuperFrame.Done = True
    End Sub


    Public Sub MakeGaussian(qc As Single, ByRef G() As Single)
        Dim n As Single
        qc = qc * SuperFrame.DimX


        Dim LG(SuperFrame.DimX - 1, SuperFrame.DimY - 1) As Single
        For x = 0 To SuperFrame.DimX - 1
            For y = 0 To SuperFrame.DimY - 1
                LG(x, y) = Math.Exp((-(x - SuperFrame.DimX / 2) ^ 2 - (y - SuperFrame.DimY / 2) ^ 2) / (2 * qc ^ 2))

            Next
        Next

        'saveSinglePage32("G.tif", LG)
        LG = fftshift(LG)
        'saveSinglePage32("G_shifted.tif", LG)
        Dim GC(SuperFrame.DimX * SuperFrame.DimY - 1) As Single
        ReDim G(SuperFrame.DimX * SuperFrame.DimY * 2 - 1)

        Dim p As Integer = 0


        For j = 0 To SuperFrame.DimY - 1
            For i = 0 To SuperFrame.DimX - 1
                '(SuperFrame.DimX * SuperFrame.DimY) ^ 2 This is to take  into account the required normalization for FFT directions
                GC(p) = (LG(i, j)) / (SuperFrame.DimX * SuperFrame.DimY) ^ 2
                G(2 * p) = GC(p)
                G(2 * p + 1) = 0
                p = p + 1
            Next
        Next

    End Sub
End Class
