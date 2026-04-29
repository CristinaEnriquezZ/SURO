<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Visualizacion.aspx.cs" Inherits="SURO2.Visualizacion" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/Personalizado.css" rel="stylesheet" />
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css" rel="stylesheet" />

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphContenido" runat="server">

    <asp:HiddenField ID="hfIDOficio" runat="server" />
    <asp:HiddenField ID="hfIDUsuario" runat="server" />
    <div class="card-header py-3">
    <h4 id="lblTitulo" runat="server" class="text-primary font-weight-bold">
    <i class="fas fa-folder-open mr-2"></i>Listado de Oficios Externos
</h4>
</div>


        <div class="card-body">
            <!-- Filtros en una sola fila -->
            <div class="row mb-4 align-items-end">
                <!-- Buscar por Folio -->
                <div class="col-md-6">
                    <label class="form-label">Buscar por Folio Oficio/Secretario, N° Oficio o Remitente</label>
                    <asp:Panel runat="server" DefaultButton="btnBuscar">
                        <div class="input-group">
                            <asp:TextBox ID="txtBuscarMultiple" runat="server"
                                CssClass="form-control"
                                placeholder="Ej. Folio/Secretario, N° Oficio o Remitente" />
                            <asp:Button ID="btnBuscar" runat="server"
                                Text="Buscar"
                                CssClass="btn btn-primary"
                                OnClick="btnBuscar_Click" />
                        </div>
                    </asp:Panel>
                </div>




                <!-- Filtrar por Estatus -->
                <div class="col-md-4">
                    <label class="form-label ms-3">Filtrar por Estatus</label>
                    <div class="d-flex align-items-center ms-3">
                        <asp:DropDownList ID="ddlEstatus" runat="server"
                            CssClass="form-select"
                            AutoPostBack="true"
                            Width="200"
                            OnSelectedIndexChanged="ddlEstatus_SelectedIndexChanged">
                            <asp:ListItem Text="-- Todos --" Value="" />
                            <asp:ListItem Text="Capturado" Value="Capturado" />
                            <asp:ListItem Text="Recibido" Value="Recibido" />
                            <asp:ListItem Text="Turnado" Value="Turnado" />
                            <asp:ListItem Text="En Proceso" Value="En Proceso" />
                            <asp:ListItem Text="Concluido" Value="Concluido" />
                        </asp:DropDownList>
                    </div>
                </div>
</div>
         

            <!-- Grid para OFICIOS INTERNOS -->
 <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
                     <ContentTemplate>
        <div class="table-wrapper">
            <asp:GridView ID="gvInternos"  HeaderStyle-CssClass="grid-header" runat="server"
                AutoGenerateColumns="False"
                CssClass="table table-bordered"
                DataKeyNames="ID"
                EnableViewState="false"
                OnRowDataBound="gvOficios_RowDataBound"
                OnRowCommand="gvOficios_RowCommand"
                OnPageIndexChanging="gvOficios_PageIndexChanging"
                AllowPaging="true" PageSize="10" Width="100%" Style="table-layout: fixed;"
                Visible="false">
                <Columns>
                    <asp:BoundField DataField="NumeroOficio" HeaderText="N° Oficio" />
                    <asp:BoundField DataField="Remitente" HeaderText="Remitente" />
                    <asp:BoundField DataField="TipoDocumento" HeaderText="Tipo de Documento" />
                    <asp:TemplateField HeaderText="Estatus">
                        <ItemTemplate>
                            <asp:Label ID="lblEstatus" runat="server" Text='<%# Eval("Estatus") %>'></asp:Label>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:LinkButton ID="btnVerDetalles" runat="server"
                                CommandName="VerDetalles"
                                CommandArgument='<%# Eval("ID") %>'
                                CssClass="btn btn-outline-primary btn-sm">
                    <i class="fas fa-eye"></i> Ver
                            </asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
            </div>
        </ContentTemplate>

    </asp:UpdatePanel>
</div>


            <!-- GridView con UpdatePanel -->
            <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
                <ContentTemplate>
                    <div class="table-wrapper">
                        <asp:GridView ID="gvOficios" HeaderStyle-CssClass="grid-header" runat="server"
                            AutoGenerateColumns="False" DataKeyNames="ID" Width="100%" Style="table-layout: fixed;"
                            EnableViewState="false"
                            CssClass="table table-bordered" OnRowDataBound="gvOficios_RowDataBound"
                            AllowPaging="true" PageSize="10" OnRowCommand="gvOficios_RowCommand"
                            OnPageIndexChanging="gvOficios_PageIndexChanging" OnRowCreated="gvOficios_RowCreated">

                            <Columns>
                                <asp:BoundField DataField="ID" HeaderText="ID" Visible="False" />
                                <%--<asp:BoundField DataField="FolioCaptura" HeaderText="Folio Captura" HeaderStyle-Width="100px" />--%>
                                <asp:BoundField DataField="FolioOficio" HeaderText="Folio Oficio/Secretario" />
                                <asp:BoundField DataField="NumeroOficio" HeaderText="N° Oficio" />
                                <asp:BoundField DataField="Remitente" HeaderText="Remitente" />
                                <asp:BoundField DataField="LugarRemitente" HeaderText="Lugar Remitente" />
                                <asp:BoundField DataField="MunicipioRemitente" HeaderText="Municipio" />
                                <asp:BoundField DataField="NivelAtencion" HeaderText="Nivel de Atención" />
                                <asp:BoundField DataField="TipoDocumento" HeaderText="Tipo de Documento" />

                                <asp:TemplateField HeaderText="Estatus">
                                    <HeaderStyle Width="100px" />
                                    <ItemStyle Width="100px" />
                                    <ItemTemplate>
                                        <asp:Label ID="lblEstatus" runat="server" Text='<%# Eval("Estatus") %>'></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Acciones">
                                    <HeaderStyle Width="160px" />
                                    <ItemStyle Width="160px" />
                                    <ItemTemplate>
                                        <div runat="server" id="divAcciones" class="d-flex gap-1 justify-content-center">
                                            <asp:LinkButton ID="btnVerDetalles" runat="server"
                                                CssClass="btn btn-outline-primary btn-sm btn-lowercase"
                                                CommandName="VerDetalles"
                                                CommandArgument='<%# Eval("ID") %>'>
                                                <i class="fas fa-eye fa-sm mr-1"></i>Ver
                                            </asp:LinkButton>
                                            <asp:HyperLink ID="btnEditar" runat="server"
                                                CssClass="btn btn-outline-warning btn-sm ml-1 btn-lowercase">
                                                <i class="fas fa-pen fa-sm mr-1"></i> Editar
                                            </asp:HyperLink>
                                        </div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>

                        </asp:GridView>
                    </div>
                </ContentTemplate>

            </asp:UpdatePanel>
       
   


</asp:Content>
