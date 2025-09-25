' OverflowInventoryForm.Designer.vb
' Clean Designer code for OverflowInventoryForm (mapOverlay removed)

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OverflowInventoryForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(OverflowInventoryForm))
        Me.txtPart = New System.Windows.Forms.TextBox()
        Me.txtQty = New System.Windows.Forms.TextBox()
        Me.cmbLocation = New System.Windows.Forms.ComboBox()
        Me.btnAdd = New System.Windows.Forms.Button()
        Me.btnExport = New System.Windows.Forms.Button()
        Me.dgvOverflow = New System.Windows.Forms.DataGridView()
        Me.PrintPreviewDialog1 = New System.Windows.Forms.PrintPreviewDialog()
        CType(Me.dgvOverflow, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'txtPart
        '
        Me.txtPart.Location = New System.Drawing.Point(16, 16)
        Me.txtPart.Name = "txtPart"
        Me.txtPart.Size = New System.Drawing.Size(140, 20)
        Me.txtPart.TabIndex = 0
        '
        'txtQty
        '
        Me.txtQty.Location = New System.Drawing.Point(162, 16)
        Me.txtQty.Name = "txtQty"
        Me.txtQty.Size = New System.Drawing.Size(68, 20)
        Me.txtQty.TabIndex = 1
        '
        'cmbLocation
        '
        Me.cmbLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbLocation.FormattingEnabled = True
        Me.cmbLocation.Location = New System.Drawing.Point(236, 16)
        Me.cmbLocation.Name = "cmbLocation"
        Me.cmbLocation.Size = New System.Drawing.Size(104, 21)
        Me.cmbLocation.TabIndex = 2
        '
        'btnAdd
        '
        Me.btnAdd.Location = New System.Drawing.Point(346, 16)
        Me.btnAdd.Name = "btnAdd"
        Me.btnAdd.Size = New System.Drawing.Size(64, 21)
        Me.btnAdd.TabIndex = 3
        Me.btnAdd.Text = "Add"
        Me.btnAdd.UseVisualStyleBackColor = True
        '
        'btnExport
        '
        Me.btnExport.Location = New System.Drawing.Point(416, 16)
        Me.btnExport.Name = "btnExport"
        Me.btnExport.Size = New System.Drawing.Size(96, 21)
        Me.btnExport.TabIndex = 4
        Me.btnExport.Text = "Print Report"
        Me.btnExport.UseVisualStyleBackColor = True
        '
        'dgvOverflow
        '
        Me.dgvOverflow.AllowUserToAddRows = False
        Me.dgvOverflow.AllowUserToDeleteRows = False
        Me.dgvOverflow.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dgvOverflow.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvOverflow.Location = New System.Drawing.Point(16, 52)
        Me.dgvOverflow.Name = "dgvOverflow"
        Me.dgvOverflow.ReadOnly = True
        Me.dgvOverflow.RowTemplate.Height = 25
        Me.dgvOverflow.Size = New System.Drawing.Size(840, 240)
        Me.dgvOverflow.TabIndex = 5
        '
        'PrintPreviewDialog1
        '
        Me.PrintPreviewDialog1.AutoScrollMargin = New System.Drawing.Size(0, 0)
        Me.PrintPreviewDialog1.AutoScrollMinSize = New System.Drawing.Size(0, 0)
        Me.PrintPreviewDialog1.ClientSize = New System.Drawing.Size(400, 300)
        Me.PrintPreviewDialog1.Enabled = True
        Me.PrintPreviewDialog1.Icon = CType(resources.GetObject("PrintPreviewDialog1.Icon"), System.Drawing.Icon)
        Me.PrintPreviewDialog1.Name = "PrintPreviewDialog1"
        Me.PrintPreviewDialog1.Visible = False
        '
        'OverflowInventoryForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(872, 520)
        Me.Controls.Add(Me.dgvOverflow)
        Me.Controls.Add(Me.btnExport)
        Me.Controls.Add(Me.btnAdd)
        Me.Controls.Add(Me.cmbLocation)
        Me.Controls.Add(Me.txtQty)
        Me.Controls.Add(Me.txtPart)
        Me.Name = "OverflowInventoryForm"
        Me.Text = "Overflow Inventory Tracker"
        CType(Me.dgvOverflow, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents txtPart As TextBox
    Friend WithEvents txtQty As TextBox
    Friend WithEvents cmbLocation As ComboBox
    Friend WithEvents btnAdd As Button
    Friend WithEvents btnExport As Button
    Friend WithEvents dgvOverflow As DataGridView
    Friend WithEvents PrintPreviewDialog1 As PrintPreviewDialog
End Class
