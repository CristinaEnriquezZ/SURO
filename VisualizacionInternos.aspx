<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="VisualizacionInternos.aspx.cs" Inherits="SURO2.VisualizacionInternos" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
  <link href="css/Personalizado.css" rel="stylesheet" />
  <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css" rel="stylesheet" />
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphContenido" runat="server">
      <!-- ESTILOS SOLO PARA ESTA PÁGINA -->
    <style>
        /* Contenedor de la barra de pestañas */
        .chrome-tabs-wrapper {
            margin-top: 6px;
            margin-bottom: 10px;
        }

        /* Barra gris tipo pestañas de Chrome */
        .chrome-tabs {
            display: inline-flex;
            align-items: flex-end;
            background: #e5e7eb;
            border-radius: 8px 8px 0 0;
            padding: 3px;
            border: 1px solid #d0d0d0;
            border-bottom: none;
        }

        /* Estilo base de cada pestaña */
        .chrome-tab {
            position: relative;
            padding: 6px 18px;
            font-size: 0.9rem;
            border-radius: 8px 8px 0 0;
            border: 1px solid transparent;
            background: transparent;
            color: #555 !important;
            text-decoration: none !important;
            cursor: pointer;
            display: inline-flex;
            align-items: center;
            gap: 6px;
            transition: background 0.15s ease,
                        color 0.15s ease,
                        box-shadow 0.15s ease,
                        border-color 0.15s ease;
        }

        /* Pestaña inactiva */
        .chrome-tab-inactive {
            background: transparent;
            color: #666 !important;
        }
        .chrome-tab-inactive:hover {
            background: rgba(255,255,255,0.4);
        }

        /* Pestaña activa (tipo Chrome) */
        .chrome-tab-active {
            background: #ffffff;
            color: #1976d2 !important;
            border-color: #d0d0d0;
            box-shadow: 0 -1px 3px rgba(0,0,0,0.08);
            z-index: 2;
        }

        /* Iconos un poco más pequeños */
        .chrome-tab i {
            font-size: 0.85rem;
        }

        /* Quitar bordes feos al enfocar */
        .chrome-tab:focus {
            outline: none;
            box-shadow: none;
        }
    </style>
        <asp:HiddenField ID="hfIDOficio" runat="server" />
    <asp:HiddenField ID="hfIDUsuario" runat="server" />
    <div class="card-header py-3">
    <h4 id="lblTitulo" runat="server" class="text-primary font-weight-bold">
    <i class="fas fa-folder-open mr-2"></i>Listado de Oficios Internos
</h4>

        <div class="chrome-tabs-wrapper mb-3">
            <div class="chrome-tabs">
                <asp:LinkButton ID="btnModoCapturados" runat="server"
                    CssClass="chrome-tab"
                    OnClick="btnModoCapturados_Click"
                    CausesValidation="false">
            <i class="fas fa-user-edit me-1"></i> Capturados por mí
                </asp:LinkButton>

                <asp:LinkButton ID="btnModoTurnados" runat="server"
                    CssClass="chrome-tab"
                    OnClick="btnModoTurnados_Click"
                    CausesValidation="false">
            <i class="fas fa-share-square me-1"></i> Turnados a mi área
                </asp:LinkButton>
            </div>
        </div>



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
                   <asp:HiddenField ID="hfModoInterno" runat="server"/>

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
                            CssClass="table table-bordered" OnRowDataBound="gvOficios_RowDataBound"
                            AllowPaging="true" PageSize="10" OnRowCommand="gvOficios_RowCommand"
                            OnPageIndexChanging="gvOficios_PageIndexChanging" OnRowCreated="gvOficios_RowCreated">

                            <Columns>
                                <asp:BoundField DataField="ID" HeaderText="ID" Visible="False" />
                                <asp:BoundField DataField="NumeroOficio" HeaderText="N° Oficio" />
                                <asp:BoundField DataField="Remitente" HeaderText="Remitente" />
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
