<%@ Page Title="Externos" Async="true" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Externos.aspx.cs" Inherits="SURO2.Externos" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

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

        <!-- Custom styles for this template-->
        <link href="css/sb-admin-2.min.css" rel="stylesheet" />

    
    </asp:Content>    
    
    
   <asp:Content ContentPlaceHolderID="cphContenido" runat="server">
        <div class="container">
            
            <section class="vh-100 gradient-custom">
               
                    
                <div class="container-fluid">
                    <div class="row justify-content-center align-items-center h-100">
                        <div class="col-12 ">
                            <div class="card shadow-2-strong card-registration" style="border-radius: 15px;">
                                <div class="card-body p-6 p-md-5">
                                    <h3 class="mb-6 pb-2 pb-md-0 mb-md-6">Registro de Oficios Externos</h3>
                                    <div class="row">                                      
                                        <div class="col-md-6 mb-4">
                                            <div class="form-outline">
                                                <label class="form-label" for="FechaOficio">Fecha Oficio</label>
                                                <asp:TextBox runat="server" Textmode="date" ID="FechaOficio" class="form-control" />
                                                <span id="errorFechaOficio" class="text-danger" style="display:none;">La fecha del oficio es requerida.</span>
                                            </div>
                                        </div>
                                    </div>
                                    <hr class="hr hr-blurry" />
                                    <h5 class="mb-6 pb-2 pb-md-0 mb-md-6">Remitente</h5>
                                    <div class="row">
                                        <div class="col-md-6 mb-4 d-flex align-items-center">
                                            <div class="form-outline datepicker w-100">
                                                <label for="Remitente" class="form-label">Nombre</label>
                                                <asp:TextBox runat="server" class="form-control" ID="Remitente" data-val="true" data-val-required="Este campo es requerido." ></asp:TextBox>
                                                <span id="errorRemitente" class="text-danger" style="display:none;">El nombre del remitente es requerido.</span>
                                            </div>

                                        </div>
                                        <div class="col-md-6 mb-4 d-flex align-items-center">
                                            <div class="form-outline datepicker w-100">
                                                <label for="Lugar" class="form-label">Lugar</label>
                                                <asp:TextBox runat="server" class="form-control" ID="Lugar" />
                                                <span id="errorLugar" class="text-danger" style="display:none;">El lugar es requerido.</span>
                                            </div>
                                        </div>
                                        <div class="col-md-6 mb-4 d-flex align-items-center">
                                            <div class="form-outline datepicker w-100">
                                                <label for="Telefono" class="form-label">Teléfono</label>
                                                <asp:TextBox runat="server" class="form-control" ID="Telefono" />
                                                 <span id="errorTelefono" class="text-danger" style="display:none;">El número de Teléfono es requerido.</span>
                                            </div>
                                        </div>
                                        <div class="col-md-6 mb-4 d-flex align-items-center">
                                            <div class="form-outline datepicker w-100">
                                                <label for="Correo" class="form-label">Correo Electrónico</label>
                                                <asp:TextBox runat="server" class="form-control" ID="Correo" type="email" />
                                            </div>
                                        </div>
                                    </div>

                                    <div class="row">
                                        <div class="col-md-6 mb-4 d-flex align-items-center">
                                            <div class="form-group">
                                                <label class="form-label select-label" for="Municipio">Municipio</label>
                                                <asp:DropDownList ID="Municipios"  runat="server" DataSourceID="dsMunicipio" class="form-control" DataTextField="Municipio" DataValueField="Clave" ClientIDMode="Static"></asp:DropDownList>
                                                <span id="errorMunicipio" class="text-danger" style="display:none;">Debe seleccionar un municipio.</span>
                                            </div>
                                            <div id="otroMunicipioContainer" class="form-group" style="display: none;">
                                                    <label for="otroMunicipioInput" class="form-label select-label">Especificar otro:</label>
                                                    <asp:TextBox runat="server" type="text" ID="otroMunicipioInput" name="otroMunicipioInput" class="form-control" ClientIDMode="Static"/>
                                                    <span id="errorOtro" class="text-danger" style="display:none;">Debe de ingresar un valor para Otro.</span>
                                        </div>  
                                        </div>

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
                                    <div id="FolioBox" name="FolioBox" class="col-md-6 mb-4 d-flex align-items-center">
                                        <div class="form-outline datepicker w-100">
                                            <label for="Folio" class="form-label">Folio</label>
                                            <asp:TextBox runat="server" class="form-control" ID="Folio" />
                                            <span id="errorFolio" class="text-danger" style="display:none;">El folio es requerido.</span>
                                        </div>
                                    </div>
                                    <div class="col-md-6 mb-4 d-flex align-items-center">
                                        <div class="form-outline datepicker w-100">
                                            <label for="NoOficio" class="form-label">No. Oficio</label>
                                            <asp:TextBox runat="server" class="form-control" ID="NoOficio" />
                                            <span id="errorNoOficio" class="text-danger" style="display:none;">El número de oficio es requerido.</span>
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
                                            <span id="errortam" class="text-danger" style="display:none;"></span>
                                            <br /><br />
                                            <asp:Label ID="lblMessageUpload" runat="server" ForeColor="Green"></asp:Label>

                                        </div>
                                    </div>

                                </div>
                                <div class="row">
                                    <div class="text-end">
                                        <asp:SqlDataSource ID="dsMunicipio" runat="server" ConnectionString="<%$ ConnectionStrings:SUROConnectionString %>" ProviderName="<%$ ConnectionStrings:SUROConnectionString.ProviderName %>" SelectCommand="SELECT '--Seleccione--' AS Municipio, 0 AS Clave UNION SELECT { fn CONCAT(Municipio, '') } AS Municipio, Clave FROM Municipios ORDER BY Clave, Municipio"></asp:SqlDataSource>
                                        <asp:SqlDataSource ID="dsTipoDocumento" runat="server" ConnectionString="<%$ ConnectionStrings:SUROConnectionString %>" SelectCommand="SELECT '--Seleccione--' AS TipoDocumento, 0 AS ID UNION SELECT { fn CONCAT(TipoDocumento, '') } AS TipoDocumento, ID FROM Tipo_Doc ORDER BY ID, TipoDocumento"></asp:SqlDataSource>
                                        <asp:Button runat="server" ID="btnGuardar" OnClientClick="return validarFormulario();" Width="150px"  Text="Guardar" class="btn btn-primary btn-lg" OnClick="btnGuardar_Click" />
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

               // Limpiar todos los mensajes de error al inicio
               document.querySelectorAll('.text-danger').forEach(function (span) {
                   span.style.display = 'none';
               });
               document.querySelectorAll('.form-control').forEach(function (input) {
                   input.classList.remove('is-invalid');
               });

               // Validar Fecha Oficio
               var fechaOficio = document.getElementById('<%= FechaOficio.ClientID %>');
        if (fechaOficio.value.trim() === '') {
            document.getElementById('errorFechaOficio').style.display = 'block';
            fechaOficio.classList.add('is-invalid');
            isValid = false;
        }

        // Validar Remitente
        var remitente = document.getElementById('<%= Remitente.ClientID %>');
        if (remitente.value.trim() === '') {
            document.getElementById('errorRemitente').style.display = 'block';
            remitente.classList.add('is-invalid');
            isValid = false;
        }

        // Validar Lugar
        var lugar = document.getElementById('<%= Lugar.ClientID %>');
        if (lugar.value.trim() === '') {
            document.getElementById('errorLugar').style.display = 'block';
            lugar.classList.add('is-invalid');
            isValid = false;
        }

               // Validar Municipio
               var municipio = document.getElementById('<%= Municipios.ClientID %>');
               if (municipio.value === '0') { // Revisa el DataValueField de la opción "--Seleccione--"
                   document.getElementById('errorMunicipio').style.display = 'block';
                   municipio.classList.add('is-invalid');
                   isValid = false;
               }

               //Validar Otro estado
               var otro = document.getElementById('<%= otroMunicipioInput.ClientID %>');
               var municipio = document.getElementById('<%= Municipios.ClientID %>')
               if (otro.value === '' && municipio.value === '68') {
                   document.getElementById('errorOtro').style.display = 'block';
                   otro.classList.add('is-invalid');
                   isValid = false;
               }


        // Validar Tipo de Documento
        var tipoDocumento = document.getElementById('<%= ddlDocumento.ClientID %>');
        if (tipoDocumento.value === '0') { // Revisa el DataValueField de la opción "--Seleccione--"
            document.getElementById('errorTipoDocumento').style.display = 'block';
            tipoDocumento.classList.add('is-invalid');
            isValid = false;
        }
        
               // Validar Folio
               var folio = document.getElementById('<%= Folio.ClientID %>');
               var errorFolio = document.getElementById('errorFolio');

               if (folio.value.trim() === '') {
                   // Si está vacío
                   errorFolio.innerText = 'El campo folio no puede estar vacío.';
                   errorFolio.style.display = 'block';
                   folio.classList.add('is-invalid');
                   isValid = false;
               } else if (isNaN(folio.value)) {
                   // Si contiene letras u otros caracteres no numéricos
                   errorFolio.innerText = 'El folio solo puede contener números.';
                   errorFolio.style.display = 'block';
                   folio.classList.add('is-invalid');
                   isValid = false;
               } else {
                   // Si la validación es correcta
                   errorFolio.style.display = 'none';
                   folio.classList.remove('is-invalid');
               }
        
        // Validar No. Oficio
        var noOficio = document.getElementById('<%= NoOficio.ClientID %>');
        if (noOficio.value.trim() === '') {
            document.getElementById('errorNoOficio').style.display = 'block';
            noOficio.classList.add('is-invalid');
            isValid = false;
        }
        
        // Validar Asunto
        var asunto = document.getElementById('<%= Asunto.ClientID %>');
        if (asunto.value.trim() === '') {
            document.getElementById('errorAsunto').style.display = 'block';
            asunto.classList.add('is-invalid');
            isValid = false;
        }

               //Validar Telefono del remitente
       var telefono = document.getElementById('<%= Telefono.ClientID %>')
               if (telefono.value.trim() === '') {
                   document.getElementById('errorTelefono').style.display = 'block';
                   telefono.classList.add('is-invalid');
                   isValid = false;
               }





               var archivoOficio = document.getElementById('<%= ArchivoOficio.ClientID %>');
               if (archivoOficio.files.length === 0) {
                   document.getElementById('errortam').style.display = 'block';
                   archivoOficio.classList.add('is-invalid');
                   isValid = false;
               } else {
                   // Validar que el archivo no sea mayor a 20 MB
                   var maxSize = 20 * 1024 * 1024; // 20 MB en bytes
                   if (archivoOficio.files[0].size > maxSize) {
                       document.getElementById('errortam').innerText = 'El archivo no debe ser mayor a 20 MB.';
                       document.getElementById('errortam').style.display = 'block';
                       archivoOficio.classList.add('is-invalid');
                       isValid = false;
                   } else {
                       document.getElementById('errortam').style.display = 'none';
                       archivoOficio.classList.remove('is-invalid');
                   }
               }

               // Devuelve el resultado de la validación
               return isValid;


           }

           document.addEventListener('DOMContentLoaded', function () {
               // Obtener el DropDownList de ASP.NET usando su ClientID

               const ddlMunicipios = document.getElementById('<%= Municipios.ClientID %>');
                const otroMunicipioContainer = document.getElementById('otroMunicipioContainer');
                const otroMunicipioInput = document.getElementById('otroMunicipioInput');

                function toggleOtroInput() {
                    const selectedOption = ddlMunicipios.options[ddlMunicipios.selectedIndex];

                    // La comparación se hace con el texto visible de la opción
                    if (selectedOption.text.toUpperCase() === 'OTRO') {
                        otroMunicipioContainer.style.display = 'block';
                        otroMunicipioInput.focus();
                    } else {
                        otroMunicipioContainer.style.display = 'none';
                        otroMunicipioInput.value = ''; // Limpiar el valor si no es "Otro"
                    }
                }

                // Agregar el event listener para el evento 'change'
                ddlMunicipios.addEventListener('change', toggleOtroInput);

                // Llamar la función una vez al cargar la página, por si el valor inicial es "OTRO"
                toggleOtroInput();
           });

       </script>
          
        
     </asp:Content> 


