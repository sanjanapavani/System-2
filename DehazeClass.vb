Public Class DehazeClass
    Public radius, weight As Single
    Dim W, H As Integer
    Dim Blure As FFTW_VB_Real
    Public R(), G(), B() As Single
    Dim ConfineBytes() As Byte
    Dim FF() As Single
    Public GammaY As Single
    Public Rb(), Gb(), Bb() As Single
    Public Sub New(W As Integer, H As Integer, radius As Single, weight As Single, GammaY As Single)
        Me.W = W
        Me.H = H
        Me.GammaY = GammaY
        Me.radius = radius
        Me.weight = weight

        Blure = New FFTW_VB_Real(W, H, radius, 2)
        ReDim R(W * H - 1), G(W * H - 1), B(W * H - 1)
        ReDim Rb(W * H - 1), Gb(W * H - 1), Bb(W * H - 1)
        ReDim FF(W * H - 1)

        CreateConfineByteMatrix()
    End Sub
    Public Sub CreateConfineByteMatrix()
        ReDim ConfineBytes(100000)

        For i = 0 To 255
            ConfineBytes(i) = i
        Next

        For i = 255 To 100000
            ConfineBytes(i) = 255
        Next

    End Sub
    Public Function Apply(bytes As Byte()) As Byte()

        Dim i, j As Integer

        Dim GammaMax As Single = 255 / (255 ^ GammaY)

        i = 0
        For y = 0 To H - 1
            For x = 0 To W - 1

                B(i) = bytes(j + 2)
                G(i) = bytes(j + 1)
                R(i) = bytes(j)

                'R(i) = R(i) ^ GammaY * GammaMax
                'G(i) = G(i) ^ GammaY * GammaMax
                'B(i) = B(i) ^ GammaY * GammaMax

                i += 1
                j += 3
            Next

        Next

        Blure.UpLoad(R)
        Blure.Process_FT_MTF()
        Blure.DownLoad(Rb)

        Blure.UpLoad(G)
        Blure.Process_FT_MTF()
        Blure.DownLoad(Gb)

        Blure.UpLoad(B)
        Blure.Process_FT_MTF()
        Blure.DownLoad(Bb)

        i = 0
        j = 0


        For y = 0 To H - 1
            For x = 0 To W - 1

                Rb(i) = ((R(i) - Rb(i) * weight) / (1 - weight)) * FlatFieldG(i)
                Gb(i) = ((G(i) - Gb(i) * weight) / (1 - weight)) * FlatFieldG(i)
                Bb(i) = ((B(i) - Bb(i) * weight) / (1 - weight)) * FlatFieldG(i)

                'Rb(i) = ((R(i) - Rb(i) * weight) / (1 - weight))
                'Gb(i) = ((G(i) - Gb(i) * weight) / (1 - weight))
                'Bb(i) = ((B(i) - Bb(i) * weight) / (1 - weight))


                'Rb(i) = R(i)
                'Gb(i) = G(i)
                'Bb(i) = B(i)

                If Rb(i) > 255 Then Rb(i) = 255
                If Gb(i) > 255 Then Gb(i) = 255
                If Bb(i) > 255 Then Bb(i) = 255

                If Rb(i) < 0 Then Rb(i) = 0
                If Gb(i) < 0 Then Gb(i) = 0
                If Bb(i) < 0 Then Bb(i) = 0

                bytes(j + 2) = Bb(i)
                bytes(j + 1) = Gb(i)
                bytes(j) = Rb(i)


                i += 1
                j += 3
            Next
        Next
        Return bytes
    End Function


    Public Function Apply(bmp As Bitmap) As Bitmap
        Dim bytes() As Byte
        Dim Processedbmp As New Bitmap(W, H, Imaging.PixelFormat.Format24bppRgb)
        BitmapToBytes(bmp, bytes)
        'Apply(bytes)

        byteToBitmap(bytes, Processedbmp)
        Return Processedbmp
    End Function

End Class
