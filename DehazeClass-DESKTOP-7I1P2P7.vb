Public Class DehazeClass
    Public radius, weight As Single
    Dim W, H As Integer
    Dim Blure As FFTW_VB_Real
    Public R(), G(), B() As Single
    Dim ConfineBytes() As Byte
    Dim FF() As Single
    Public Rb(), Gb(), Bb() As Single
    Public Sub New(W As Integer, H As Integer, radius As Single, weight As Single)
        Me.W = W
        Me.H = H

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
    Public Sub Apply(ByRef bytes As Byte())

        Dim i, j As Integer

        i = 0
        For y = 0 To H - 1
            For x = 0 To W - 1
                'because it is tiff
                R(i) = bytes(j + 2)
                G(i) = bytes(j + 1)
                B(i) = bytes(j)
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
                Rb(i) = (R(i) - Rb(i) * weight) / (1 - weight)
                Gb(i) = (G(i) - Gb(i) * weight) / (1 - weight)
                Bb(i) = (B(i) - Bb(i) * weight) / (1 - weight) + 40

                If Rb(i) > 255 Then Rb(i) = 255
                If Gb(i) > 255 Then Gb(i) = 255
                If Bb(i) > 255 Then Bb(i) = 255

                If Rb(i) < 0 Then Rb(i) = 0
                If Gb(i) < 0 Then Gb(i) = 0
                If Bb(i) < 0 Then Bb(i) = 0

                bytes(j + 2) = Rb(i)
                bytes(j + 1) = Gb(i)
                bytes(j) = Bb(i)


                i += 1
                j += 3
            Next
        Next

    End Sub


    Public Function Apply(bmp As Bitmap) As Bitmap
        Dim bytes() As Byte
        Dim Processedbmp As New Bitmap(W, H, Imaging.PixelFormat.Format24bppRgb)
        BitmapToBytes(bmp, bytes)

        Dim i, j As Integer

        i = 0
        For y = 0 To H - 1
            For x = 0 To W - 1
                'because it is tiff
                R(i) = bytes(j + 2)
                G(i) = bytes(j + 1)
                B(i) = bytes(j)
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
                Rb(i) = (R(i) - Rb(i) * weight) / (1 - weight)
                Gb(i) = (G(i) - Gb(i) * weight) / (1 - weight)
                Bb(i) = (B(i) - Bb(i) * weight) / (1 - weight)

                If Rb(i) > 255 Then Rb(i) = 255
                If Gb(i) > 255 Then Gb(i) = 255
                If Bb(i) > 255 Then Bb(i) = 255




                If Rb(i) < 0 Then Rb(i) = 0
                If Gb(i) < 0 Then Gb(i) = 0
                If Bb(i) < 0 Then Bb(i) = 0

                bytes(j) = (Bb(i))
                bytes(j + 1) = (Gb(i))
                bytes(j + 2) = (Rb(i))


                i += 1
                j += 3
            Next
        Next
        byteToBitmap(bytes, Processedbmp)
        Return Processedbmp
    End Function

End Class
