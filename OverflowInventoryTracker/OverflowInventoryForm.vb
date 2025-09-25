Imports System
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Printing
Imports iTextSharp.text
Imports iTextSharp.text.pdf

Public Class OverflowInventoryForm

    ' ===== Models =====
    Public Class InventoryItem
        Public Property PartNumber As String
        Public Property Quantity As Integer
        Public Property Location As String
        Public Property Age As Integer ' In days
    End Class

    Private Class OverflowItem
        Public Property PartNumber As String
        Public Property Quantity As Integer
        Public Property Location As String
        Public Property DateAdded As Date

        Public ReadOnly Property AgeInDays As Integer
            Get
                Return (Date.Today - DateAdded).Days
            End Get
        End Property
    End Class

    ' ===== State =====
    Private OverflowList As New List(Of OverflowItem)
    Private SaveFile As String = "overflow_data.txt"
    Private _suppressSelectionEvents As Boolean = False

    ' Dynamic controls (no Designer edits needed)
    Private WithEvents btnBlankReport As New Button()
    Private WithEvents txtSearch As New TextBox()
    Private WithEvents btnSearch As New Button()
    Private WithEvents lblSearch As New Label()

    ' Interactable map control (requires InteractableMap.vb)
    Private map As New InteractableMap()

    ' ===== Form Load =====
    Private Sub OverflowInventoryForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Window sizing and anti-flicker
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.ClientSize = New Size(1200, 800)
        Me.MinimumSize = New Size(1000, 650)
        Me.DoubleBuffered = True

        ' --- DataGridView layout & autosizing ---
        dgvOverflow.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        dgvOverflow.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvOverflow.MultiSelect = True
        dgvOverflow.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvOverflow.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
        dgvOverflow.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        EnableDoubleBuffering(dgvOverflow)

        ' Grid columns (base data columns)
        dgvOverflow.ColumnCount = 4
        dgvOverflow.Columns(0).Name = "Part Number"
        dgvOverflow.Columns(1).Name = "Quantity"
        dgvOverflow.Columns(2).Name = "Location"
        dgvOverflow.Columns(3).Name = "Age (Days)"

        ' Action columns
        Dim editBtn As New DataGridViewButtonColumn() With {.Name = "Edit", .Text = "Edit", .UseColumnTextForButtonValue = True, .MinimumWidth = 70}
        Dim deleteBtn As New DataGridViewButtonColumn() With {.Name = "Delete", .Text = "Delete", .UseColumnTextForButtonValue = True, .MinimumWidth = 70}
        Dim resetBtn As New DataGridViewButtonColumn() With {.Name = "Reset Dates", .Text = "Reset Dates", .UseColumnTextForButtonValue = True, .MinimumWidth = 100}
        dgvOverflow.Columns.Add(editBtn)
        dgvOverflow.Columns.Add(deleteBtn)
        dgvOverflow.Columns.Add(resetBtn)

        ' Proportional fill weights (wider Part Number, narrower numbers)
        dgvOverflow.Columns("Part Number").FillWeight = 220
        dgvOverflow.Columns("Quantity").FillWeight = 80
        dgvOverflow.Columns("Location").FillWeight = 110
        dgvOverflow.Columns("Age (Days)").FillWeight = 90
        dgvOverflow.Columns("Edit").FillWeight = 70
        dgvOverflow.Columns("Delete").FillWeight = 70
        dgvOverflow.Columns("Reset Dates").FillWeight = 120

        ' Load persisted data
        LoadFromFile()

        ' Aging refresh timer (every minute)
        Dim refreshTimer As New Timer()
        AddHandler refreshTimer.Tick, AddressOf RefreshAging
        refreshTimer.Interval = 60000
        refreshTimer.Start()

        ' Label the export button as "Print Report"
        btnExport.Text = "Print Report"

        ' Locations OF1..OF26 in the combobox
        cmbLocation.Items.Clear()
        For i As Integer = 1 To 26
            cmbLocation.Items.Add("OF" & i.ToString())
        Next

        ' --- Add "Blank Report" button next to "Print Report" ---
        btnBlankReport.Text = "Blank Report"
        btnBlankReport.Size = New Size(110, btnExport.Height)
        btnBlankReport.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.Controls.Add(btnBlankReport)

        ' --- Add Search controls (top-right) ---
        lblSearch.Text = "Search Part:"
        lblSearch.AutoSize = True
        lblSearch.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.Controls.Add(lblSearch)

        txtSearch.Size = New Size(200, btnExport.Height)
        txtSearch.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        AddHandler txtSearch.KeyDown, AddressOf TxtSearch_KeyDown
        Me.Controls.Add(txtSearch)

        btnSearch.Text = "Search"
        btnSearch.Size = New Size(80, btnExport.Height)
        btnSearch.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.Controls.Add(btnSearch)

        ' Position the right-side tools relative to current form size
        LayoutTopRightTools()

        ' --- Place the interactive map under the grid ---
        map.Location = New Point(dgvOverflow.Left, dgvOverflow.Bottom + 12)
        map.Size = New Size(dgvOverflow.Width, Math.Max(200, Me.ClientSize.Height - (dgvOverflow.Bottom + 24)))
        map.Anchor = AnchorStyles.Left Or AnchorStyles.Top Or AnchorStyles.Right Or AnchorStyles.Bottom
        Me.Controls.Add(map)
        map.BringToFront()

        ' ---------- Define zones (no PNG required) ----------
        Dim zones As New List(Of InteractableMap.ZoneInfo)

        ' Layout tuning knobs
        Dim marginX As Single = CSng(0.03)
        Dim marginRight As Single = CSng(0.03)
        Dim topY As Single = CSng(0.06)
        Dim topH As Single = CSng(0.24)

        Dim midY As Single = CSng(0.4)
        Dim midH As Single = CSng(0.12)

        Dim bottomY As Single = CSng(0.62)
        Dim bottomH As Single = CSng(0.28)
        Dim bottomSideNI As Single = CSng(0.06) ' non-inventory side margins in bottom row

        ' --- TOP ROW: OF1..OF6 on left half ---
        Dim topInvX As Single = marginX
        Dim topInvW As Single = CSng(0.5) - marginX
        Dim topSlotW As Single = topInvW / CSng(6.0)
        For i = 0 To 5
            zones.Add(New InteractableMap.ZoneInfo With {
                .Name = "OF" & (i + 1).ToString(),
                .BoundsPct = New RectangleF(topInvX + i * topSlotW, topY, topSlotW - CSng(0.002), topH),
                .IsInventory = True
            })
        Next

        ' Right half (non-inventory)
        zones.Add(New InteractableMap.ZoneInfo With {
            .Name = "X",
            .BoundsPct = New RectangleF(CSng(0.5) + CSng(0.01), topY, CSng(0.47) - marginRight, topH),
            .IsInventory = False
        })

        ' --- MIDDLE ROW (drive-thru, non-inventory) ---
        zones.Add(New InteractableMap.ZoneInfo With {
            .Name = "DRIVE",
            .BoundsPct = New RectangleF(marginX, midY, CSng(1.0) - marginX - marginRight, midH),
            .IsInventory = False
        })

        ' --- BOTTOM ROW ---
        zones.Add(New InteractableMap.ZoneInfo With {
            .Name = "X",
            .BoundsPct = New RectangleF(marginX, bottomY, bottomSideNI - marginX, bottomH),
            .IsInventory = False
        })

        ' Center band: OF7..OF26 (20 slots)
        Dim centerX As Single = bottomSideNI
        Dim centerW As Single = CSng(1.0) - bottomSideNI - bottomSideNI
        Dim bottomSlotW As Single = centerW / CSng(20.0)
        For i = 0 To 19
            zones.Add(New InteractableMap.ZoneInfo With {
                .Name = "OF" & (7 + i).ToString(),
                .BoundsPct = New RectangleF(centerX + i * bottomSlotW, bottomY, bottomSlotW - CSng(0.002), bottomH),
                .IsInventory = True
            })
        Next

        zones.Add(New InteractableMap.ZoneInfo With {
            .Name = "X",
            .BoundsPct = New RectangleF(CSng(1.0) - bottomSideNI, bottomY, bottomSideNI - marginRight, bottomH),
            .IsInventory = False
        })

        map.SetZones(zones)

        ' Map -> Grid selection
        AddHandler map.ZoneClicked, AddressOf Map_ZoneClicked

        ' Initial grid populate / map summary build
        RefreshGrid()

        ' Prevent default “first row” selection on startup
        dgvOverflow.ClearSelection()
        dgvOverflow.CurrentCell = Nothing
    End Sub

    ' Arrange the right-side tools (called on load and on resize)
    Private Sub LayoutTopRightTools()
        Dim margin As Integer = 10
        Dim topY As Integer = btnExport.Top
        Dim rightX As Integer = Me.ClientSize.Width - margin

        ' btnSearch at far right
        btnSearch.Location = New Point(rightX - btnSearch.Width, topY)
        rightX = btnSearch.Left - margin

        ' txtSearch to the left of btnSearch
        txtSearch.Location = New Point(rightX - txtSearch.Width, topY + (btnSearch.Height - txtSearch.Height) \ 2)
        rightX = txtSearch.Left - margin

        ' label left of txtSearch
        lblSearch.Location = New Point(rightX - lblSearch.PreferredWidth, topY + (btnSearch.Height - lblSearch.PreferredHeight) \ 2)
        rightX = lblSearch.Left - margin

        ' Blank Report sits to the right of Print Report
        btnBlankReport.Location = New Point(btnExport.Right + margin, topY)
    End Sub

    Private Sub OverflowInventoryForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        LayoutTopRightTools()

        ' Keep the map sized under the grid on resize
        map.Location = New Point(dgvOverflow.Left, dgvOverflow.Bottom + 12)
        map.Size = New Size(dgvOverflow.Width, Math.Max(200, Me.ClientSize.Height - (dgvOverflow.Bottom + 24)))
    End Sub

    ' Reduce grid flicker
    Private Sub EnableDoubleBuffering(dgv As DataGridView)
        Try
            Dim dgvType = dgv.GetType()
            Dim pi = dgvType.GetProperty("DoubleBuffered", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
            If pi IsNot Nothing Then
                pi.SetValue(dgv, True, Nothing)
            End If
        Catch
            ' ignore if not available
        End Try
    End Sub

    ' Map click selects ALL matching rows in the grid
    Private Sub Map_ZoneClicked(zName As String)
        SelectAllRowsForLocation(zName)
    End Sub

    Private Sub dgvOverflow_CellMouseDown(sender As Object, e As DataGridViewCellMouseEventArgs) Handles dgvOverflow.CellMouseDown
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

        Dim colName = dgvOverflow.Columns(e.ColumnIndex).Name
        If String.Equals(colName, "Location", StringComparison.OrdinalIgnoreCase) Then
            Dim loc = TryCast(dgvOverflow.Rows(e.RowIndex).Cells(e.ColumnIndex).Value, String)
            SelectAllRowsForLocation(loc)
        End If
    End Sub

    ' Grid selection highlights the zone on the map
    Private Sub dgvOverflow_SelectionChanged(sender As Object, e As EventArgs) Handles dgvOverflow.SelectionChanged
        If _suppressSelectionEvents Then Return

        If dgvOverflow.SelectedRows.Count > 0 Then
            Dim val = dgvOverflow.SelectedRows(0).Cells("Location").Value
            If val IsNot Nothing Then
                Dim loc As String = val.ToString()
                If Not String.IsNullOrEmpty(loc) Then
                    map.SelectedZone = loc
                End If
            End If
        End If
    End Sub

    ' ===== CRUD / Grid binding =====
    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        If txtPart.Text = "" Or txtQty.Text = "" Or cmbLocation.SelectedIndex = -1 Then
            MessageBox.Show("Please fill in all fields.")
            Exit Sub
        End If

        Dim part = txtPart.Text.Trim()
        Dim qty = CInt(txtQty.Text)
        Dim loc = cmbLocation.SelectedItem.ToString()

        ' Match by PartNumber + Location (allow same part in multiple zones)
        Dim existing = OverflowList.FirstOrDefault(Function(x) _
            String.Equals(x.PartNumber, part, StringComparison.OrdinalIgnoreCase) AndAlso
            String.Equals(x.Location, loc, StringComparison.OrdinalIgnoreCase))

        If existing IsNot Nothing Then
            If existing.Quantity <> qty Then
                existing.Quantity = qty
                existing.DateAdded = Date.Today ' treat as movement/reset age
            End If
        Else
            OverflowList.Add(New OverflowItem With {
                .PartNumber = part,
                .Quantity = qty,
                .Location = loc,
                .DateAdded = Date.Today
            })
        End If

        RefreshGrid(preserveSelection:=True)
        SaveToFile()
        ClearInputs()
    End Sub

    Private Sub SelectAllRowsForLocation(loc As String)
        If String.IsNullOrEmpty(loc) Then Return

        _suppressSelectionEvents = True
        dgvOverflow.ClearSelection()
        Dim firstIndex As Integer = -1

        For Each row As DataGridViewRow In dgvOverflow.Rows
            Dim val = TryCast(row.Cells("Location").Value, String)
            If val IsNot Nothing AndAlso String.Equals(val, loc, StringComparison.OrdinalIgnoreCase) Then
                row.Selected = True
                If firstIndex = -1 Then firstIndex = row.Index
            End If
        Next
        _suppressSelectionEvents = False

        If firstIndex >= 0 Then
            dgvOverflow.FirstDisplayedScrollingRowIndex = firstIndex
            dgvOverflow.CurrentCell = dgvOverflow.Rows(firstIndex).Cells(0)
        End If
    End Sub

    Private Sub dgvOverflow_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvOverflow.CellContentClick
        If e.RowIndex < 0 Then Exit Sub

        Dim partCell = dgvOverflow.Rows(e.RowIndex).Cells("Part Number").Value
        If partCell Is Nothing Then Exit Sub
        Dim part = partCell.ToString()

        Dim locCell = dgvOverflow.Rows(e.RowIndex).Cells("Location").Value
        If locCell Is Nothing Then Exit Sub
        Dim loc = locCell.ToString()

        ' Identify the item by Part + Location
        Dim item = OverflowList.FirstOrDefault(Function(x) _
            String.Equals(x.PartNumber, part, StringComparison.OrdinalIgnoreCase) AndAlso
            String.Equals(x.Location, loc, StringComparison.OrdinalIgnoreCase))
        If item Is Nothing Then Exit Sub

        Dim colName = dgvOverflow.Columns(e.ColumnIndex).Name
        If colName = "Edit" Then
            txtPart.Text = item.PartNumber
            txtQty.Text = item.Quantity.ToString()
            cmbLocation.SelectedItem = item.Location

        ElseIf colName = "Delete" Then
            If MessageBox.Show("Delete " & item.PartNumber & " (" & item.Location & ")?", "Confirm", MessageBoxButtons.YesNo) = DialogResult.Yes Then
                OverflowList.Remove(item)
                RefreshGrid(preserveSelection:=False)
                SaveToFile()
            End If

        ElseIf colName = "Reset Dates" Then
            If MessageBox.Show("Reset date for " & item.PartNumber & " (" & item.Location & ")?", "Confirm", MessageBoxButtons.YesNo) = DialogResult.Yes Then
                item.DateAdded = Date.Today
                RefreshGrid(preserveSelection:=True)
                SaveToFile()
            End If
        End If
    End Sub

    Private Sub RefreshGrid(Optional preserveSelection As Boolean = False)
        ' Collect selection (Part+Location) & scroll/current cell BEFORE refresh
        Dim selKeys As New List(Of (String, String))()
        Dim firstDisplayed As Integer = -1
        Dim currentKey As (String, String) = (Nothing, Nothing)
        Dim hadSelection As Boolean = dgvOverflow.SelectedRows.Count > 0

        If preserveSelection AndAlso hadSelection Then
            For Each r As DataGridViewRow In dgvOverflow.SelectedRows
                Dim pn = TryCast(r.Cells("Part Number").Value, String)
                Dim lc = TryCast(r.Cells("Location").Value, String)
                If pn IsNot Nothing AndAlso lc IsNot Nothing Then
                    selKeys.Add((pn, lc))
                End If
            Next

            If dgvOverflow.CurrentCell IsNot Nothing Then
                Dim r = dgvOverflow.CurrentCell.RowIndex
                If r >= 0 AndAlso r < dgvOverflow.Rows.Count Then
                    Dim pn = TryCast(dgvOverflow.Rows(r).Cells("Part Number").Value, String)
                    Dim lc = TryCast(dgvOverflow.Rows(r).Cells("Location").Value, String)
                    If pn IsNot Nothing AndAlso lc IsNot Nothing Then
                        currentKey = (pn, lc)
                    End If
                End If
            End If

            If dgvOverflow.FirstDisplayedScrollingRowIndex >= 0 Then
                firstDisplayed = dgvOverflow.FirstDisplayedScrollingRowIndex
            End If
        End If

        _suppressSelectionEvents = True
        Try
            dgvOverflow.SuspendLayout()
            dgvOverflow.Rows.Clear()

            For Each item In OverflowList
                dgvOverflow.Rows.Add(item.PartNumber, item.Quantity, item.Location, item.AgeInDays)
            Next
        Finally
            dgvOverflow.ResumeLayout()
            _suppressSelectionEvents = False
        End Try

        ' Re-apply selection & caret ONLY if we had selection before
        If preserveSelection AndAlso hadSelection AndAlso selKeys.Count > 0 Then
            Dim firstIndex As Integer = -1

            For Each row As DataGridViewRow In dgvOverflow.Rows
                Dim pn = TryCast(row.Cells("Part Number").Value, String)
                Dim lc = TryCast(row.Cells("Location").Value, String)
                If selKeys.Any(Function(k) String.Equals(k.Item1, pn, StringComparison.OrdinalIgnoreCase) AndAlso
                                         String.Equals(k.Item2, lc, StringComparison.OrdinalIgnoreCase)) Then
                    row.Selected = True
                    If firstIndex = -1 Then firstIndex = row.Index
                End If
            Next

            ' Restore caret to previously current row, if still exists
            If currentKey.Item1 IsNot Nothing Then
                For Each row As DataGridViewRow In dgvOverflow.Rows
                    Dim pn = TryCast(row.Cells("Part Number").Value, String)
                    Dim lc = TryCast(row.Cells("Location").Value, String)
                    If String.Equals(pn, currentKey.Item1, StringComparison.OrdinalIgnoreCase) AndAlso
                       String.Equals(lc, currentKey.Item2, StringComparison.OrdinalIgnoreCase) Then
                        dgvOverflow.CurrentCell = row.Cells(0)
                        Exit For
                    End If
                Next
            End If

            ' Restore scroll
            If firstDisplayed >= 0 AndAlso firstDisplayed < dgvOverflow.Rows.Count Then
                dgvOverflow.FirstDisplayedScrollingRowIndex = firstDisplayed
            ElseIf firstIndex >= 0 Then
                dgvOverflow.FirstDisplayedScrollingRowIndex = firstIndex
            End If
        Else
            ' Keep nothing selected (avoid auto row 0)
            dgvOverflow.ClearSelection()
            dgvOverflow.CurrentCell = Nothing
        End If

        ' Build summary for map (location -> list of (part, qty))
        Dim summary As New Dictionary(Of String, List(Of Tuple(Of String, Integer)))(StringComparer.OrdinalIgnoreCase)
        For Each item In OverflowList
            If Not summary.ContainsKey(item.Location) Then
                summary(item.Location) = New List(Of Tuple(Of String, Integer))()
            End If
            summary(item.Location).Add(Tuple.Create(item.PartNumber, item.Quantity))
        Next
        map.SetInventorySummary(summary)
    End Sub

    Private Sub RefreshAging(sender As Object, e As EventArgs)
        Dim hadSelection As Boolean = dgvOverflow.SelectedRows.Count > 0
        RefreshGrid(preserveSelection:=hadSelection)
    End Sub

    Private Sub ClearInputs()
        txtPart.Clear()
        txtQty.Clear()
        cmbLocation.SelectedIndex = -1
    End Sub

    ' ===== Persistence =====
    Private Sub SaveToFile()
        Using writer As New StreamWriter(SaveFile, False)
            For Each item In OverflowList
                writer.WriteLine(String.Format("{0},{1},{2},{3:yyyy-MM-dd}",
                                               item.PartNumber, item.Quantity, item.Location, item.DateAdded))
            Next
        End Using
    End Sub

    Private Sub LoadFromFile()
        If Not File.Exists(SaveFile) Then Return

        Try
            Using reader As New StreamReader(SaveFile)
                While Not reader.EndOfStream
                    Dim line = reader.ReadLine()
                    If String.IsNullOrWhiteSpace(line) Then Continue While

                    Dim parts = line.Split(","c)
                    If parts.Length < 4 Then Continue While

                    Dim partNum As String = parts(0).Trim()
                    Dim qty As Integer
                    Dim loc As String = parts(2).Trim()
                    Dim dt As Date

                    Dim okQty As Boolean = Integer.TryParse(parts(1).Trim(), qty)
                    Dim okDate As Boolean = Date.TryParseExact(
                        parts(3).Trim(),
                        "yyyy-MM-dd",
                        Globalization.CultureInfo.InvariantCulture,
                        Globalization.DateTimeStyles.None,
                        dt
                    )

                    If okQty AndAlso okDate AndAlso partNum <> "" AndAlso loc <> "" Then
                        OverflowList.Add(New OverflowItem With {
                            .PartNumber = partNum,
                            .Quantity = qty,
                            .Location = loc,
                            .DateAdded = dt
                        })
                    End If
                End While
            End Using
        Catch ex As Exception
            MessageBox.Show("Error reading save file: " & ex.Message, "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ' ===== Reports =====
    Public Sub GeneratePDFReport(inventoryData As List(Of InventoryItem), outputPath As String)
        Dim doc As New Document(PageSize.A4, 40, 40, 40, 40)
        PdfWriter.GetInstance(doc, New FileStream(outputPath, FileMode.Create))
        doc.Open()

        ' Fonts
        Dim titleFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10)
        Dim headerFont = FontFactory.GetFont("Helvetica", 10, iTextSharp.text.Font.BOLD, BaseColor.WHITE)
        Dim sectionFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10)
        Dim bodyFont = FontFactory.GetFont("Helvetica", 10)

        ' Title
        doc.Add(New Paragraph("Overflow Inventory Report", titleFont))
        doc.Add(New Paragraph("Generated on " & DateTime.Now.ToString("MMMM dd, yyyy"), bodyFont))
        doc.Add(Chunk.NEWLINE)

        ' Sort OF1..OF26 numerically
        Dim grouped = inventoryData.
            GroupBy(Function(i) i.Location).
            OrderBy(Function(g)
                        Dim n As Integer
                        If Integer.TryParse(g.Key.Replace("OF", ""), n) Then
                            Return n
                        Else
                            Return Integer.MaxValue
                        End If
                    End Function)

        Dim mkCell As Func(Of String, Integer, PdfPCell) =
            Function(text As String, align As Integer) As PdfPCell
                Dim c As New PdfPCell(New Phrase(text, bodyFont))
                c.Border = iTextSharp.text.Rectangle.BOX
                c.Padding = 5
                c.MinimumHeight = 18
                c.HorizontalAlignment = align
                Return c
            End Function

        For Each group In grouped
            doc.Add(New Paragraph("Location: " & group.Key, sectionFont))
            doc.Add(Chunk.NEWLINE)

            Dim table As New PdfPTable(4)
            table.WidthPercentage = 100
            table.SetWidths({30, 20, 20, 30})

            Dim headers = {"Part Number", "Quantity", "Age (Days)", "Manual Count"}
            For Each col In headers
                Dim h As New PdfPCell(New Phrase(col, headerFont))
                h.BackgroundColor = New BaseColor(100, 100, 100)
                h.HorizontalAlignment = Element.ALIGN_CENTER
                h.Padding = 5
                h.Border = iTextSharp.text.Rectangle.BOX
                table.AddCell(h)
            Next

            Dim totalQty As Integer = 0
            For Each item In group
                table.AddCell(mkCell(item.PartNumber, Element.ALIGN_LEFT))
                table.AddCell(mkCell(item.Quantity.ToString(), Element.ALIGN_CENTER))
                table.AddCell(mkCell(item.Age.ToString(), Element.ALIGN_CENTER))
                table.AddCell(mkCell("", Element.ALIGN_LEFT)) ' Manual Count
                totalQty += item.Quantity
            Next

            Dim totalLabel = New PdfPCell(New Phrase("Total Quantity:", headerFont))
            totalLabel.Border = iTextSharp.text.Rectangle.TOP_BORDER
            totalLabel.PaddingTop = 6
            totalLabel.HorizontalAlignment = Element.ALIGN_RIGHT
            table.AddCell(totalLabel)

            Dim totalValue = New PdfPCell(New Phrase(totalQty.ToString(), headerFont))
            totalValue.Border = iTextSharp.text.Rectangle.TOP_BORDER
            totalValue.PaddingTop = 6
            totalValue.HorizontalAlignment = Element.ALIGN_CENTER
            table.AddCell(totalValue)

            Dim filler1 As New PdfPCell(New Phrase(""))
            filler1.Border = iTextSharp.text.Rectangle.TOP_BORDER
            filler1.PaddingTop = 6
            table.AddCell(filler1)

            Dim filler2 As New PdfPCell(New Phrase(""))
            filler2.Border = iTextSharp.text.Rectangle.TOP_BORDER
            filler2.PaddingTop = 6
            table.AddCell(filler2)

            doc.Add(table)
            doc.Add(Chunk.NEWLINE)
        Next

        doc.Close()
    End Sub

    Public Sub GenerateBlankPDFReport(outputPath As String)
        Dim doc As New Document(PageSize.A4, 40, 40, 40, 40)
        PdfWriter.GetInstance(doc, New FileStream(outputPath, FileMode.Create))
        doc.Open()

        ' Fonts: match existing
        Dim titleFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10)
        Dim headerFont = FontFactory.GetFont("Helvetica", 10, iTextSharp.text.Font.BOLD, BaseColor.WHITE)
        Dim sectionFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10)
        Dim bodyFont = FontFactory.GetFont("Helvetica", 10)

        doc.Add(New Paragraph("Overflow Inventory Blank Report", titleFont))
        doc.Add(New Paragraph("Generated on " & DateTime.Now.ToString("MMMM dd, yyyy"), bodyFont))
        doc.Add(Chunk.NEWLINE)

        Dim mkCell As Func(Of String, Integer, PdfPCell) =
            Function(text As String, align As Integer) As PdfPCell
                Dim c As New PdfPCell(New Phrase(text, bodyFont))
                c.Border = iTextSharp.text.Rectangle.BOX
                c.Padding = 5
                c.MinimumHeight = 20
                c.HorizontalAlignment = align
                Return c
            End Function

        For i As Integer = 1 To 26
            Dim locationCode As String = "OF" & i.ToString()

            doc.Add(New Paragraph("Location: " & locationCode, sectionFont))
            doc.Add(Chunk.NEWLINE)

            Dim table As New PdfPTable(4)
            table.WidthPercentage = 100
            table.SetWidths({30, 20, 20, 30})

            Dim headers = {"Part Number", "Quantity", "Age (Days)", "Manual Count"}
            For Each col In headers
                Dim h As New PdfPCell(New Phrase(col, headerFont))
                h.BackgroundColor = New BaseColor(100, 100, 100)
                h.HorizontalAlignment = Element.ALIGN_CENTER
                h.Padding = 5
                h.Border = iTextSharp.text.Rectangle.BOX
                table.AddCell(h)
            Next

            For j As Integer = 1 To 5
                table.AddCell(mkCell("", Element.ALIGN_LEFT))
                table.AddCell(mkCell("", Element.ALIGN_CENTER))
                table.AddCell(mkCell("", Element.ALIGN_CENTER))
                table.AddCell(mkCell("", Element.ALIGN_LEFT))
            Next

            Dim totalLabel As New PdfPCell(New Phrase("Total Quantity:", headerFont))
            totalLabel.Border = iTextSharp.text.Rectangle.TOP_BORDER
            totalLabel.PaddingTop = 6
            totalLabel.HorizontalAlignment = Element.ALIGN_RIGHT
            table.AddCell(totalLabel)

            Dim totalValue As New PdfPCell(New Phrase("", headerFont))
            totalValue.Border = iTextSharp.text.Rectangle.TOP_BORDER
            totalValue.PaddingTop = 6
            totalValue.HorizontalAlignment = Element.ALIGN_CENTER
            table.AddCell(totalValue)

            Dim filler1 As New PdfPCell(New Phrase(""))
            filler1.Border = iTextSharp.text.Rectangle.TOP_BORDER
            filler1.PaddingTop = 6
            table.AddCell(filler1)

            Dim filler2 As New PdfPCell(New Phrase(""))
            filler2.Border = iTextSharp.text.Rectangle.TOP_BORDER
            filler2.PaddingTop = 6
            table.AddCell(filler2)

            doc.Add(table)
            doc.Add(Chunk.NEWLINE)
        Next

        doc.Close()
    End Sub

    ' ===== Buttons =====
    Private Sub btnExport_Click(sender As Object, e As EventArgs) Handles btnExport.Click
        Dim inventoryData As New List(Of InventoryItem)
        For Each item In OverflowList
            inventoryData.Add(New InventoryItem With {
                .PartNumber = item.PartNumber,
                .Quantity = item.Quantity,
                .Location = item.Location,
                .Age = item.AgeInDays
            })
        Next

        Dim saveDialog As New SaveFileDialog()
        saveDialog.Filter = "PDF files (*.pdf)|*.pdf"
        saveDialog.FileName = "OverflowInventoryReport.pdf"
        If saveDialog.ShowDialog() = DialogResult.OK Then
            GeneratePDFReport(inventoryData, saveDialog.FileName)
            MessageBox.Show("PDF report generated successfully.", "Report Created", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Process.Start(saveDialog.FileName)
        End If
    End Sub

    Private Sub btnBlankReport_Click(sender As Object, e As EventArgs) Handles btnBlankReport.Click
        Dim saveDialog As New SaveFileDialog()
        saveDialog.Filter = "PDF files (*.pdf)|*.pdf"
        saveDialog.FileName = "OverflowInventoryBlankReport.pdf"
        If saveDialog.ShowDialog() = DialogResult.OK Then
            GenerateBlankPDFReport(saveDialog.FileName)
            MessageBox.Show("Blank PDF report generated successfully.", "Report Created", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Process.Start(saveDialog.FileName)
        End If
    End Sub

    ' ===== Search =====
    Private Sub btnSearch_Click(sender As Object, e As EventArgs) Handles btnSearch.Click
        RunSearch()
    End Sub

    Private Sub TxtSearch_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            e.SuppressKeyPress = True
            RunSearch()
        End If
    End Sub

    Private Sub RunSearch()
        Dim query = txtSearch.Text.Trim()
        If String.IsNullOrEmpty(query) Then
            MessageBox.Show("Enter a Part Number to search.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        _suppressSelectionEvents = True
        dgvOverflow.ClearSelection()
        Dim matches As New List(Of Integer)
        For Each row As DataGridViewRow In dgvOverflow.Rows
            Dim pn = TryCast(row.Cells("Part Number").Value, String)
            If pn IsNot Nothing AndAlso pn.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 Then
                row.Selected = True
                If matches.Count = 0 Then
                    matches.Add(row.Index)
                End If
            End If
        Next
        _suppressSelectionEvents = False

        If matches.Count > 0 Then
            dgvOverflow.FirstDisplayedScrollingRowIndex = matches(0)
            dgvOverflow.CurrentCell = dgvOverflow.Rows(matches(0)).Cells(0)
            Dim loc = TryCast(dgvOverflow.Rows(matches(0)).Cells("Location").Value, String)
            If Not String.IsNullOrEmpty(loc) Then map.SelectedZone = loc
        Else
            MessageBox.Show("No matching part numbers found.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

End Class
