Public Class StackImage
    Public data()(,) As Single
    Public Bitmp() As FastBMP
    Public Width As Integer
    Public Height As Integer
    Public Depth As Integer
    Public Min() As Single
    Public Max() As Single
    Public threshold() As Single
    Public Bitdepth As Byte
    Dim HistBin As Integer = 256
    Public ib(), ic() As Integer
    Dim Histogram() As Integer

    Public Sub New(W As Integer, H As Integer, D As Integer, format As Imaging.PixelFormat)
        ReDim data(D - 1)
        ReDim Bitmp(D - 1)
        ReDim Min(D - 1)
        ReDim Max(D - 1)
        ReDim threshold(D - 1)
        ReDim ib(D - 1)
        ReDim ic(D - 1)
        For i = 0 To D - 1
            ReDim data(i)(W - 1, H - 1)
            Bitmp(i) = New FastBMP(W, H, format)
        Next
        Width = W : Height = H : Depth = D

    End Sub

    Public Sub FindMax(index)
        For y = 0 To Height - 1
            For x = 0 To Width - 1
                If data(index)(x, y) > Max(index) Then Max(index) = data(index)(x, y)
            Next
        Next
    End Sub
    Private Sub MakeHistogram(index As Integer)
        FindMax(index)
        ReDim Histogram(HistBin - 1)

        For j = 0 To Height - 1 Step 2
            For i = 0 To Width - 1 Step 2

                Histogram(data(index)(i, j) * (HistBin - 1) / Max(index)) += 1

            Next
        Next
        ' We never have zero intensity  this make  it possible  to find the min on the SetIbIc using the min function
        Histogram(0) = Histogram(1)
    End Sub
    Public Sub SetIbIc(index As Integer)
        MakeHistogram(index)
        Dim Hmax As Integer = 0
        Dim HCum(HistBin - 1) As Integer
        Dim HCumMax, HCumMin As Integer
        'Accumulates the histogram bins
        Dim i As Integer
        For i = 0 To HistBin - 1
            For ii = 0 To i
                HCum(i) += Histogram(ii) / HistBin
            Next
        Next

        HCumMax = HCum.Max
        HCumMin = HCum.Min

        For i = HistBin - 1 To 0 Step -1
            If Histogram(i) > HCumMax Then Exit For
        Next
        ic(index) = i * 1.5
        If ic(index) > HistBin Then ic(index) = HistBin

        For i = 0 To ic(index)
            If Histogram(i) > HCumMin Then Exit For
        Next
        ib(index) = i

        ib(index) = ib(index) * Max(index) / HistBin
        ic(index) = ic(index) * Max(index) / HistBin


    End Sub

    Public Sub CoolBright(index)
        ' SetIbIc(index)
        Bitmp(index).Unlock()

        Dim length As Integer = Bitmp(index).bytes.Length

        Dim c As Integer
        Dim p As Integer = 0
        If ib(index) = ic(index) Then ib(index) = 0 : ic(index) = 255
        For i = 0 To length - 1
            c = (Bitmp(index).byteCopy(i) - ib(index)) / (ic(index) - ib(index)) * 255
            If c < 0 Then c = 0
            If c > 255 Then c = 255
            Bitmp(index).byteCopy(i) = c
        Next
        Bitmp(index).lock()
    End Sub

    Public Sub Export(filename As String)
        'SaveJaggedArray(data, filename)
    End Sub
End Class