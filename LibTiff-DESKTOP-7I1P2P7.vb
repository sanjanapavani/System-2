Imports BitMiracle.LibTiff.Classic
Imports Microsoft.VisualBasic.CompilerServices

Module LibTiff

    Public Sub ReadMultiPage(ByVal inputName As String, ByRef ImgArray(,,) As Single, ByRef width As Integer, ByRef Height As Integer, ByRef Z As Integer)

        Z = GetPages(inputName)
        Using img As Tiff = Tiff.Open(inputName, "r")
            Dim res As FieldValue() = img.GetField(TiffTag.IMAGELENGTH)
            Height = res(0).ToInt()

            res = img.GetField(TiffTag.IMAGEWIDTH)
            width = res(0).ToInt()

            ReDim ImgArray(width - 1, Height - 1, Z)

            res = img.GetField(TiffTag.BITSPERSAMPLE)
            Dim bpp As Short = res(0).ToShort()
            ' If bpp <> 16 Then
            'Return
            'End If

            res = img.GetField(TiffTag.SAMPLESPERPIXEL)
            Dim spp As Short = res(0).ToShort()
            If spp <> 1 Then
                Return
            End If


            Dim stride As Integer = img.ScanlineSize()
            Dim buffer As Byte() = New Byte(stride - 1) {}



            Dim ii As Integer



            If bpp = 16 Then
                For k = 0 To Z

                    For j As Integer = 0 To Height - 1

                        img.ReadScanline(buffer, j)
                        ii = 0

                        For i = 0 To stride - 1 Step 2
                            ImgArray(ii, j, k) = buffer(i) + buffer(i + 1) * 256
                            ii += 1
                        Next
                    Next
                    img.ReadDirectory()
                Next

            Else
                For k = 0 To Z - 1

                    For j As Integer = 0 To Height - 1

                        img.ReadScanline(buffer, j)
                        ii = 0

                        For i = 0 To stride - 1 Step 4
                            ImgArray(ii, j, k) = BitConverter.ToSingle(buffer, i)
                            ii += 1
                        Next
                    Next
                    img.ReadDirectory()
                Next

            End If

        End Using


    End Sub


    Public Function ReadMultiJaggedArray(ByVal inputName As String, ByRef image As Single()(,)) As Byte

        Dim Depth As Integer = GetPages(inputName)
        Using img As Tiff = Tiff.Open(inputName, "r")
            Dim res As FieldValue() = img.GetField(TiffTag.IMAGELENGTH)
            Dim Height As Integer = res(0).ToInt()

            res = img.GetField(TiffTag.IMAGEWIDTH)
            Dim Width As Integer = res(0).ToInt()

            ReDim image(Depth - 1)

            For z = 0 To Depth - 1
                ReDim image(z)(Width - 1, Height - 1)
            Next

            res = img.GetField(TiffTag.BITSPERSAMPLE)
            Dim bpp As Short = res(0).ToShort()
            ' If bpp <> 16 Then
            'Return
            'End If

            res = img.GetField(TiffTag.SAMPLESPERPIXEL)
            Dim spp As Short = res(0).ToShort()
            If spp <> 1 Then
                Return 0
            End If


            Dim stride As Integer = img.ScanlineSize()
            Dim buffer As Byte() = New Byte(stride - 1) {}



            Dim ii As Integer




            Select Case bpp
                Case 8
                    For k = 0 To Depth - 1

                        For j As Integer = 0 To Height - 1

                            img.ReadScanline(buffer, j)
                            ii = 0

                            For i = 0 To stride - 1
                                image(k)(ii, j) = buffer(i)
                                ii += 1
                            Next
                        Next
                        img.ReadDirectory()
                    Next
                Case 16
                    For k = 0 To Depth - 1

                        For j As Integer = 0 To Height - 1

                            img.ReadScanline(buffer, j)
                            ii = 0

                            For i = 0 To stride - 1 Step 2
                                image(k)(ii, j) = buffer(i) + buffer(i + 1) * 256
                                ii += 1
                            Next
                        Next
                        img.ReadDirectory()
                    Next

                Case 32
                    For k = 0 To Depth - 1

                        For j As Integer = 0 To Height - 1

                            img.ReadScanline(buffer, j)
                            ii = 0

                            For i = 0 To stride - 1 Step 4
                                image(k)(ii, j) = BitConverter.ToSingle(buffer, i)
                                ii += 1
                            Next
                        Next
                        img.ReadDirectory()
                    Next
            End Select

            Return bpp
        End Using


    End Function



    Public Function ReadMultiPage32(ByVal inputName As String) As Single(,,)


        Dim Z As Integer = GetPages(inputName)
        Using img As Tiff = Tiff.Open(inputName, "r")
            Dim res As FieldValue() = img.GetField(TiffTag.IMAGELENGTH)
            Dim height As Integer = res(0).ToInt()

            res = img.GetField(TiffTag.IMAGEWIDTH)
            Dim width As Integer = res(0).ToInt()

            Dim ImgArray(width - 1, height - 1, Z) As Single

            res = img.GetField(TiffTag.BITSPERSAMPLE)
            Dim bpp As UInt32 = res(0).ToShort()
            If bpp <> 32 Then
                Return Nothing
            End If

            res = img.GetField(TiffTag.SAMPLESPERPIXEL)
            Dim spp As Short = res(0).ToShort()
            If spp <> 1 Then
                Return Nothing
            End If


            Dim stride As Integer = img.ScanlineSize()
            Dim buffer As Byte() = New Byte(stride - 1) {}



            Dim ii As Integer

            For k = 0 To Z - 1

                For j As Integer = 0 To height - 1

                    img.ReadScanline(buffer, j)
                    ii = 0
                    For i = 0 To stride - 1 Step 4
                        ImgArray(ii, j, k) = buffer(i) + buffer(i + 1) * 256 + buffer(i + 2) * 256 * 256 + buffer(i + 3) * 256 * 256 * 256
                        ii += 1
                    Next
                Next
                img.ReadDirectory()
            Next
            Return ImgArray
        End Using


    End Function

    Public Sub SaveMultipageTiff(ByVal filename As String, ByVal Frame(,,) As Single)

        Dim width As Integer = Frame.GetUpperBound(0)
        Dim height As Integer = Frame.GetUpperBound(1)
        Dim numberOfPages As Integer = Frame.GetUpperBound(2)

        Const samplesPerPixel As Integer = 1
        Const bitsPerSample As Integer = 16

        Dim samples As UShort() = New UShort(width - 1) {}

        'Tiff.SetTagExtender(AddressOf TagExtender)

        Using output As Tiff = Tiff.Open(filename, "w")


            For page As Integer = 0 To numberOfPages
                output.SetField(TiffTag.IMAGEWIDTH, width / samplesPerPixel)
                output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
                output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)

                'output.SetField(TIFFTAG_COMMENT, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))
                'output.SetField(TIFFTAG_TAG, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))

                ' specify that it's a page within the multipage file
                output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE)
                ' specify the page number
                output.SetField(TiffTag.PAGENUMBER, page, numberOfPages)


                For i As Integer = 0 To height - 1
                    For j As Integer = 0 To width - 1
                        samples(j) = Frame(j, i, page)
                    Next

                    Dim buf As Byte() = New Byte(samples.Length * 2 - 1) {}
                    Buffer.BlockCopy(samples, 0, buf, 0, buf.Length)
                    output.WriteScanline(buf, i)
                Next

                output.WriteDirectory()
            Next
        End Using
    End Sub


    Public Sub SaveSinglePageTiff(ByVal filename As String, stack(,) As Single)

        Const samplesPerPixel As Integer = 1
        Const bitsPerSample As Integer = 16


        Dim page As Integer
        Dim numberOfPages As Integer = 1
        Dim W As Integer = stack.GetUpperBound(0)
        Dim H As Integer = stack.GetUpperBound(1)
        Dim samples As UShort() = New UShort(W) {}

        Using output As Tiff = Tiff.Open(filename, "w")

            output.SetField(TiffTag.IMAGEWIDTH, W / samplesPerPixel)
            output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
            output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)

            ' specify that it's a page within the multipage file
            output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE)
            ' specify the page number
            output.SetField(TiffTag.PAGENUMBER, page, numberOfPages)


            For i As Integer = 0 To W
                For j As Integer = 0 To H
                    samples(j) = 0
                    samples(j) += stack(i, j)
                Next
                Dim buf As Byte() = New Byte(samples.Length * 2 - 1) {}
                Buffer.BlockCopy(samples, 0, buf, 0, buf.Length)
                output.WriteScanline(buf, i)
            Next
            output.WriteDirectory()
        End Using




    End Sub


    Public Sub SaveSinglePageTiff(ByVal filename As String, frame() As Byte, W As Integer, H As Integer)

        Dim Stack(W - 1, H - 1) As Byte

        Const samplesPerPixel As Integer = 1
        Const bitsPerSample As Integer = 8


        Dim page As Integer
        Dim numberOfPages As Integer = 1

        Dim samples As Byte() = New Byte(W - 1) {}

        Using output As Tiff = Tiff.Open(filename, "w")

            output.SetField(TiffTag.IMAGEWIDTH, W / samplesPerPixel)
            output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
            output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)

            ' specify that it's a page within the multipage file
            output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE)
            ' specify the page number
            output.SetField(TiffTag.PAGENUMBER, page, numberOfPages)

            Dim p As Integer = 0
            For i As Integer = 0 To H - 1
                For j As Integer = 0 To W - 1
                    samples(j) = frame(p)
                    p += 1
                Next
                Dim buf As Byte() = New Byte(samples.Length - 1) {}
                Buffer.BlockCopy(samples, 0, buf, 0, buf.Length)
                output.WriteScanline(buf, i)
            Next
            output.WriteDirectory()
        End Using

    End Sub

    Public Sub SaveSinglePageTiff16(ByVal filename As String, frame() As Byte, W As Integer, H As Integer)

        Dim Stack(W - 1, H - 1) As Byte

        Const samplesPerPixel As Integer = 1
        Const bitsPerSample As Integer = 16


        Dim page As Integer
        Dim numberOfPages As Integer = 1

        Dim samples As UShort() = New UShort(W) {}

        Using output As Tiff = Tiff.Open(filename, "w")

            output.SetField(TiffTag.IMAGEWIDTH, W / samplesPerPixel)
            output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
            output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)

            ' specify that it's a page within the multipage file
            output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE)
            ' specify the page number
            output.SetField(TiffTag.PAGENUMBER, page, numberOfPages)

            Dim p As Integer = 0
            For i As Integer = 0 To H - 1
                For j As Integer = 0 To W - 1
                    samples(j) = frame(p)
                    p += 1
                Next
                Dim buf As Byte() = New Byte(samples.Length * 2 - 1) {}
                Buffer.BlockCopy(samples, 0, buf, 0, buf.Length)
                output.WriteScanline(buf, i)
            Next
            output.WriteDirectory()
        End Using

    End Sub

    Public Sub SaveSinglePageTiff16(ByVal filename As String, frame() As Single, W As Integer, H As Integer)

        Dim Stack(W - 1, H - 1) As Byte

        Const samplesPerPixel As Integer = 1
        Const bitsPerSample As Integer = 16


        Dim page As Integer
        Dim numberOfPages As Integer = 1

        Dim samples As UShort() = New UShort(W) {}

        Using output As Tiff = Tiff.Open(filename, "w")

            output.SetField(TiffTag.IMAGEWIDTH, W / samplesPerPixel)
            output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
            output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)

            ' specify that it's a page within the multipage file
            output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE)
            ' specify the page number
            output.SetField(TiffTag.PAGENUMBER, page, numberOfPages)

            Dim p As Integer = 0
            For i As Integer = 0 To H - 1
                For j As Integer = 0 To W - 1
                    samples(j) = frame(p)
                    p += 1
                Next
                Dim buf As Byte() = New Byte(samples.Length * 2 - 1) {}
                Buffer.BlockCopy(samples, 0, buf, 0, buf.Length)
                output.WriteScanline(buf, i)
            Next
            output.WriteDirectory()
        End Using

    End Sub
    Public Sub saveSinglePage32(ByVal filename As String, frame() As Single, Width As Integer, Height As Integer)



        Const samplesPerPixel As Integer = 1
        Const bitsPerSample As Integer = 32

        Dim samples As Single() = New Single(Width - 1) {}

        'Tiff.SetTagExtender(AddressOf TagExtender)

        Using output As Tiff = Tiff.Open(filename, "w")



            output.SetField(TiffTag.IMAGEWIDTH, Width / samplesPerPixel)
            output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
            output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)
            output.SetField(TiffTag.SAMPLEFORMAT, SampleFormat.IEEEFP)
            'output.SetField(TIFFTAG_COMMENT, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))
            'output.SetField(TIFFTAG_TAG, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))

            ' specify that it's a page within the multipage file
            output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE)
            ' specify the page number


            Dim p As Integer = 0
            For i As Integer = 0 To Height - 1
                For j As Integer = 0 To Width - 1
                    samples(j) = frame(p)
                    p += 1
                Next

                Dim buf As Byte() = New Byte(samples.Length * 4 - 1) {}

                Buffer.BlockCopy(samples, 0, buf, 0, buf.Length)
                output.WriteScanline(buf, i)
            Next

            output.WriteDirectory()

        End Using
    End Sub



    Public Sub saveSinglePage32(ByVal filename As String, frame(,) As Single)
        Dim width As Integer = frame.GetLength(0)
        Dim height As Integer = frame.GetLength(1)


        Const samplesPerPixel As Integer = 1
        Const bitsPerSample As Integer = 32

        Dim samples As Single() = New Single(width - 1) {}

        'Tiff.SetTagExtender(AddressOf TagExtender)

        Using output As Tiff = Tiff.Open(filename, "w")



            output.SetField(TiffTag.IMAGEWIDTH, width / samplesPerPixel)
            output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
            output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)
            output.SetField(TiffTag.SAMPLEFORMAT, SampleFormat.IEEEFP)
            'output.SetField(TIFFTAG_COMMENT, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))
            'output.SetField(TIFFTAG_TAG, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))

            ' specify that it's a page within the multipage file
            output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE)
            ' specify the page number



            For i As Integer = 0 To height - 1
                For j As Integer = 0 To width - 1
                    samples(j) = frame(j, i)
                Next

                Dim buf As Byte() = New Byte(samples.Length * 4 - 1) {}

                Buffer.BlockCopy(samples, 0, buf, 0, buf.Length)
                output.WriteScanline(buf, i)
            Next

            output.WriteDirectory()

        End Using
    End Sub


    Public Sub Save_MultiTiff(ByVal Frame(,,) As Single, ByVal filename As String)

        Dim width As Integer = Frame.GetUpperBound(0)
        Dim height As Integer = Frame.GetUpperBound(1)
        Dim numberOfPages As Integer = Frame.GetUpperBound(2)

        Const samplesPerPixel As Integer = 1
        Const bitsPerSample As Integer = 16

        Dim samples As UShort() = New UShort(width - 1) {}

        'Tiff.SetTagExtender(AddressOf TagExtender)

        Using output As Tiff = Tiff.Open(filename, "w")


            For page As Integer = 0 To numberOfPages
                output.SetField(TiffTag.IMAGEWIDTH, width / samplesPerPixel)
                output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
                output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)

                'output.SetField(TIFFTAG_COMMENT, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))
                'output.SetField(TIFFTAG_TAG, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))

                ' specify that it's a page within the multipage file
                output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE)
                ' specify the page number
                output.SetField(TiffTag.PAGENUMBER, page, numberOfPages)


                For i As Integer = 0 To height - 1
                    For j As Integer = 0 To width - 1
                        samples(j) = Frame(j, i, page)
                    Next

                    Dim buf As Byte() = New Byte(samples.Length * 2 - 1) {}
                    Buffer.BlockCopy(samples, 0, buf, 0, buf.Length)
                    output.WriteScanline(buf, i)
                Next

                output.WriteDirectory()
            Next
        End Using
    End Sub


    Public Sub SaveJaggedArray(ByVal Frame()(,) As Single, ByVal filename As String)

        Dim width As Integer = Frame(0).GetUpperBound(0)
        Dim height As Integer = Frame(0).GetUpperBound(1)
        Dim numberOfPages As Integer = Frame.GetUpperBound(0)

        Const samplesPerPixel As Integer = 1
        Const bitsPerSample As Integer = 16

        Dim samples As UShort() = New UShort(width - 1) {}

        'Tiff.SetTagExtender(AddressOf TagExtender)

        Using output As Tiff = Tiff.Open(filename, "w")


            For page As Integer = 0 To numberOfPages
                output.SetField(TiffTag.IMAGEWIDTH, width / samplesPerPixel)
                output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
                output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)

                'output.SetField(TIFFTAG_COMMENT, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))
                'output.SetField(TIFFTAG_TAG, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))

                ' specify that it's a page within the multipage file
                output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE)
                ' specify the page number
                output.SetField(TiffTag.PAGENUMBER, page, numberOfPages)


                For i As Integer = 0 To height - 1
                    For j As Integer = 0 To width - 1
                        samples(j) = Frame(page)(j, i)
                    Next

                    Dim buf As Byte() = New Byte(samples.Length * 2 - 1) {}
                    Buffer.BlockCopy(samples, 0, buf, 0, buf.Length)
                    output.WriteScanline(buf, i)
                Next

                output.WriteDirectory()
            Next
            output.Close()
        End Using
    End Sub

    Public Sub SaveJaggedArray(ByVal Frame()() As Byte, width As Integer, height As Integer, ByVal filename As String)


        Dim numberOfPages As Integer = Frame.GetUpperBound(0)

        Const samplesPerPixel As Integer = 1
        Const bitsPerSample As Integer = 8

        Dim samples As Byte() = New Byte(width - 1) {}

        'Tiff.SetTagExtender(AddressOf TagExtender)

        Using output As Tiff = Tiff.Open(filename, "w")


            For page As Integer = 0 To numberOfPages
                output.SetField(TiffTag.IMAGEWIDTH, width / samplesPerPixel)
                output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
                output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)

                'output.SetField(TIFFTAG_COMMENT, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))
                'output.SetField(TIFFTAG_TAG, muse.tag_comments.Length * 2, System.Text.Encoding.Unicode.GetBytes((muse.tag_comments)))

                ' specify that it's a page within the multipage file
                output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE)
                ' specify the page number
                output.SetField(TiffTag.PAGENUMBER, page, numberOfPages)


                For i As Integer = 0 To height - 1
                    For j As Integer = 0 To width - 1
                        samples(j) = Frame(page)(j + i * width)
                    Next

                    output.WriteScanline(samples, i)
                Next

                output.WriteDirectory()
            Next
        End Using
    End Sub


    Public Function Read32(ByVal inputName As String) As Single(,)



        Using img As Tiff = Tiff.Open(inputName, "r")
            Dim res As FieldValue() = img.GetField(TiffTag.IMAGELENGTH)
            Dim height As Integer = res(0).ToInt()

            res = img.GetField(TiffTag.IMAGEWIDTH)
            Dim width As Integer = res(0).ToInt()

            Dim ImgArray(width - 1, height - 1) As Single

            res = img.GetField(TiffTag.BITSPERSAMPLE)
            Dim bpp As UInt32 = res(0).ToShort()
            If bpp <> 32 Then
                Return Nothing
            End If

            res = img.GetField(TiffTag.SAMPLESPERPIXEL)
            Dim spp As Short = res(0).ToShort()
            If spp <> 1 Then
                Return Nothing
            End If


            Dim stride As Integer = img.ScanlineSize()
            Dim buffer As Byte() = New Byte(stride - 1) {}



            Dim ii As Integer
            Dim bf(3) As Byte

            Dim max As Single
            For j As Integer = 0 To height - 1

                img.ReadScanline(buffer, j)
                ii = 0
                For i = 0 To stride - 1 Step 4
                    bf(0) = buffer(i)
                    bf(1) = buffer(i + 1)
                    bf(2) = buffer(i + 2)
                    bf(3) = buffer(i + 3)

                    ImgArray(ii, j) = BitConverter.ToSingle(bf, 0)

                    ii += 1
                Next
            Next









            Return ImgArray
        End Using


    End Function




    Private Function GetPages(Filename As String) As Integer

        Dim image As Tiff = Tiff.Open(Filename, "r")
        Dim pageCount As Integer = 0
        Do
            pageCount += 1
        Loop While image.ReadDirectory()

        Return pageCount
    End Function


    Private Sub convertBuffer(ByVal buffer As Byte(), ByVal buffer8Bit As Byte())
        Dim src As Integer = 0, dst As Integer = 0
        While src < buffer.Length
            Dim value16 As Integer = buffer(src)
            src += 1
            value16 = value16 + (CType(buffer(src), Integer) << 8)
            src += 1
            buffer8Bit(dst) = Math.Floor(value16 / 257.0 + 0.5) Mod 256
            dst += 1
        End While
    End Sub
End Module
