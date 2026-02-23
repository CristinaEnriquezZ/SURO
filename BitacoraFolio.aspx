<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="BitacoraFolio.aspx.cs" Inherits="SURO2.BitacoraFolio" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphContenido" runat="server">

    <div class="container-fluid">
        <div class="card shadow mb-4" style="border-radius: 15px;">
            <div class="card-body">

                <div class="d-flex align-items-center justify-content-between mb-3">
                    <h4 class="m-0">Minutario de Folios</h4>
                </div>

                <!-- Filtros -->
                <div class="row g-3">
                    <div class="col-md-4">
                        <label class="form-label">No. Oficio</label>
                        <asp:TextBox runat="server" ID="txtFolio" CssClass="form-control"
                            placeholder="Ej. SDR.00.001.00019/2026" />
                    </div>

                    <div class="col-md-2">
                        <label class="form-label">Registro desde</label>
                        <asp:TextBox runat="server" ID="txtFechaDesde" TextMode="Date" CssClass="form-control" />
                    </div>

                    <div class="col-md-2">
                        <label class="form-label">Registro hasta</label>
                        <asp:TextBox runat="server" ID="txtFechaHasta" TextMode="Date" CssClass="form-control" />
                    </div>

                    <div class="col-md-2 d-flex align-items-end">
                        <div class="form-check">
                            <asp:CheckBox runat="server" ID="chkSoloModificados" CssClass="form-check-input" />
                            <label class="form-check-label" for="<%= chkSoloModificados.ClientID %>">Solo modificados</label>
                        </div>
                    </div>

                    <div class="col-12 d-flex gap-2">
                        <asp:Button runat="server" ID="btnBuscar" CssClass="btn btn-primary"
                            Text="Buscar" OnClick="btnBuscar_Click" />
                        <asp:Button runat="server" ID="btnLimpiar" CssClass="btn btn-outline-secondary"
                            Text="Limpiar" OnClick="btnLimpiar_Click" />
                    </div>
                </div>

                <hr />

                <asp:Label runat="server" ID="lblMsg" CssClass="text-danger" />

                <div class="table-responsive">
                    <asp:GridView ID="gvBitacora" runat="server"
                        CssClass="table table-striped table-hover align-middle"
                        AutoGenerateColumns="False"
                        AllowPaging="True" PageSize="15"
                        OnPageIndexChanging="gvBitacora_PageIndexChanging"
                        OnRowDataBound="gvBitacora_RowDataBound"
                        GridLines="None">

                        <Columns>
                            <asp:BoundField DataField="FechaRegistro" HeaderText="Fecha registro" DataFormatString="{0:dd/MM/yyyy}"  />
                            <asp:BoundField DataField="FechaOficio" HeaderText="Fecha oficio" DataFormatString="{0:dd/MM/yyyy}" />                           
                            <asp:BoundField DataField="FolioCapturado" HeaderText="Folio capturado" />
                            <asp:TemplateField HeaderText="Fojas">
                            <ItemTemplate>
                                <asp:Label runat="server" ID="lblFojas"
                                    Text='<%# (Eval("Fojas") == null || Eval("Fojas") == DBNull.Value || Eval("Fojas").ToString().Trim() == "")
                                            ? "NA"
                                            : Eval("Fojas").ToString() %>' />
                            </ItemTemplate>
                           </asp:TemplateField>
                            <asp:BoundField DataField="Destinatario" HeaderText="Destinatario" />
                            <asp:BoundField DataField="Asunto" HeaderText="Asunto" />
                            <asp:TemplateField HeaderText="¿Modificado?">
                                <ItemTemplate>
                                    <asp:Label runat="server" ID="lblModificado" Text='<%# Eval("FueModificado") %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>

            </div>
        </div>
    </div>
</asp:Content>
