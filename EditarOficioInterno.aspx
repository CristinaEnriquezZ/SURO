<%@ Page Title="EditarOficioInterno" Async = "true" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="EditarOficioInterno.aspx.cs" Inherits="SURO2.EditarOficioInterno" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1,shrink-to-fit=no" />
<title><%: Page.Title %> - S.U.R.O</title>

<asp:PlaceHolder runat="server">
    <%: Scripts.Render("~/bundles/modernizr") %>
</asp:PlaceHolder>


<webopt:BundleReference runat="server" Path="~/Content/css" />
<link href="~/favicon.ico" rel="shortcut icon" type="image/x-icon" />

<link rel="stylesheet" href="https://code.jquery.com/ui/1.14.1/themes/base/jquery-ui.css" />


<!-- Custom fonts for this template-->
<link href="vendor/fontawesome-free/css/all.min.css" rel="stylesheet" type="text/css" />
<link
    href="https://fonts.googleapis.com/css?family=Nunito:200,200i,300,300i,400,400i,600,600i,700,700i,800,800i,900,900i"
    rel="stylesheet" />
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.5/font/bootstrap-icons.css"/>


<!-- Custom styles for this template-->
<link href="css/sb-admin-2.min.css" rel="stylesheet" />
<link href="css/Personalizado.css" rel="stylesheet" />



</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphContenido" runat="server">
          <div class="container">
      
      <section class="vh-100 gradient-custom">
   
          <div class="container-fluid">
              <div class="row justify-content-center align-items-center h-100">
                  <div class="col-12 ">
                      <div class="card shadow-2-strong card-registration" style="border-radius: 15px;">
                          <div class="card-body p-6 p-md-5">
                              <h3 class="mb-6 pb-2 pb-md-0 mb-md-6">Modificación de Oficios Internos</h3>
                              <div class="row">                              
                                  <div class="col-md-6 mb-4">
                                      <div class="form-outline">
                                          <label class="form-label" for="FechaOficio">Fecha Oficio</label>
                                          <asp:TextBox runat="server" type="date" ID="FechaOficio" class="form-control" />
                                          <span id="errorFechaOficio" class="text-danger" style="display:none;">La fecha del oficio es requerida.</span>
                                      </div>

                                  </div>
                              </div>
                              <hr class="hr hr-blurry" />
                           
                              <div class="row">                            
                                  <div id="TipoDocumento" name="TipoDocumento" class="col-md-6 mb-4 d-flex align-items-center">
                                      <div class="col-md-12">
                                          <div class="form-group">
                                              <label class="form-label select-label" for="TipoDocumento">Tipo de Documento</label>
                                              <asp:DropDownList ID="ddlDocumento"  runat="server" DataSourceID="dsTipoDocumento" class="form-control" DataTextField="TipoDocumento" DataValueField="ID"></asp:DropDownList>
                                              <span id="errorTipoDocumento" class="text-danger" style="display:none;">Debe seleccionar un tipo de documento.</span>
                                          </div>
                                      </div>
                                  </div>
                              </div>

                          <hr class="hr hr-blurry" />
                          <h5 class="mb-6 pb-2 pb-md-0 mb-md-6">Datos del Oficio</h5>
                          <div class="row">
                              <div class="col-md-6 mb-4 d-flex align-items-center">
                                  <div class="form-outline datepicker w-100">
                                      <label for="NoOficio" class="form-label">No. Oficio</label>
                                      <asp:TextBox runat="server" class="form-control" ID="NoOficio" ReadOnly="true" />
                                      <span id="errorNoOficio" class="text-danger" style="display: none;">El número de oficio es requerido.</span>
                                  </div>
                              </div>
                          </div>
                          <div class="form-group" style="width: 100%;">
                              <div class="col-lg-12">
                                  <label for="Asunto" class="form-label">Asunto</label>
                                  <asp:TextBox runat="server" class="form-control" Style="min-width: 100%;" cols="50" Height="100" ID="Asunto" Rows="10"></asp:TextBox>
                                  <span id="errorAsunto" class="text-danger" style="display:none;">El asunto es requerido.</span>
                              </div>
                          </div>
                          <div class="row">
                              <div class="col-md-6 mb-4 d-flex align-items-center">
                                  <div class="form-outline datepicker w-100">
                                      <label for="ArchivoOficio" class="form-label">Subir Archivo del Oficio</label>
                                        <asp:FileUpload  runat="server" class="form-control" id="ArchivoOficio" />
                                      <span id="errorArchivo" class="text-danger" style="display:none;">Debe seleccionar un archivo.</span>
                                  </div>
                              </div>

                          </div>
                          <div class="row">
                              <div class="text-end">
                                  <asp:SqlDataSource ID="dsMunicipio" runat="server" ConnectionString="<%$ ConnectionStrings:SUROConnectionString %>" ProviderName="<%$ ConnectionStrings:SUROConnectionString.ProviderName %>" SelectCommand="SELECT '--Seleccione--' AS Municipio, 0 AS ID UNION SELECT { fn CONCAT(Municipio, '') } AS Municipio, ID FROM Municipios ORDER BY ID, Municipio"></asp:SqlDataSource>
                                  <asp:SqlDataSource ID="dsTipoDocumento" runat="server" ConnectionString="<%$ ConnectionStrings:SUROConnectionString %>" SelectCommand="SELECT '--Seleccione--' AS TipoDocumento, 0 AS ID UNION SELECT { fn CONCAT(TipoDocumento, '') } AS TipoDocumento, ID FROM Tipo_Doc ORDER BY ID, TipoDocumento"></asp:SqlDataSource>
                                  <asp:Button runat="server" ID="btnGuardar"  OnClientClick="return validarFormulario();" Width="170px" Text="Guardar Cambios" class="btn btn-primary btn-sm" OnClick="btnGuardar_Click"/>
                              </div>
                          </div>
                          </div>
                      </div>
                  </div>
              </div>
          </div>
             
      </section>
          
  </div>

    <script type="text/javascript">
    function validarFormulario() {
        var isValid = true;

        // Limpiar todos los mensajes de error y las clases de invalidación al inicio
        document.querySelectorAll('.text-danger').forEach(function(span) {
            span.style.display = 'none';
        });
        document.querySelectorAll('.form-control').forEach(function(input) {
            input.classList.remove('is-invalid');
        });

        // Validar Fecha Oficio
        var fechaOficio = document.getElementById('<%= FechaOficio.ClientID %>');
        if (fechaOficio.value.trim() === '') {
            document.getElementById('errorFechaOficio').style.display = 'block';
            fechaOficio.classList.add('is-invalid');
            isValid = false;
        }


     

        // Validar Tipo de Documento
        var tipoDocumento = document.getElementById('<%= ddlDocumento.ClientID %>');
        if (tipoDocumento.value === '0') { // El ID 0 corresponde a la opción "--Seleccione--"
            document.getElementById('errorTipoDocumento').style.display = 'block';
            tipoDocumento.classList.add('is-invalid');
            isValid = false;
        }
        
     
     
        
        // Validar Asunto
        var asunto = document.getElementById('<%= Asunto.ClientID %>');
        if (asunto.value.trim() === '') {
            document.getElementById('errorAsunto').style.display = 'block';
            asunto.classList.add('is-invalid');
            isValid = false;
        }

  
    }
    </script>



</asp:Content>
