<%@ Page Title="Resumen Internos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ResumenInternos.aspx.cs" Inherits="SURO2.ResumenInternos" %>
<%@ Import Namespace="Newtonsoft.Json" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="head" runat="server">
    <style>
        /* ---- Altura uniforme para tarjetas ---- */
        .dashboard-cards .card.metric-card .card-body{
            min-height: 120px;
            display:flex; flex-direction:column;
            align-items:center; justify-content:center;
            padding:14px 12px;
        }
        .scrollable-card-body { max-height: 320px; overflow-y: auto; }
        .progress-bar-label {
            display:flex; justify-content:space-between; font-size:14px; font-weight:600;
            margin-bottom:2px; align-items:center; flex-wrap:wrap;
        }
        .progress { height:22px; margin-bottom:16px; background:#f3f3f3; border-radius:8px; }
        .progress-bar {
            position:relative; transition:width 0.6s ease; cursor:pointer;
            min-width:2%; overflow:visible!important; border-radius:8px;
        }
        .oficios-on-bar { margin-left:10px; color:#272727ee; font-size:15px; font-weight:800;
            background:rgba(255,255,255,0); border-radius:8px; padding:2px 10px; display:inline-block; line-height:1.2; }
        .estatus-resumen { font-size:13px; font-weight:500; color:#333; display:flex; flex-wrap:wrap; gap:7px; }

        /* ---- Chips semáforo ---- */
        .semaforo-card .chip-row{
            display:flex; gap:8px; justify-content:center;
            flex-wrap:nowrap; white-space:nowrap; overflow:hidden;
            margin-top:6px;
        }
        .semaforo-card .chip{
            display:inline-flex; align-items:center;
            border-radius:8px; padding:2px 8px;
            font-weight:600; font-size:12px;
        }
        .semaforo-card .chip .value{ margin-left:4px; font-weight:700; }
        .semaforo-card .chip.rojo    { background:#FFC7C7; color:#B71C1C; }
        .semaforo-card .chip.rojo .value{ color:#8E0000; }
        .semaforo-card .chip.naranja { background:#FFD6A5; color:#E65100; }
        .semaforo-card .chip.naranja .value{ color:#BF360C; }
        .semaforo-card .chip.amarillo{ background:#FFECB3; color:#B08900; }
        .semaforo-card .chip.amarillo .value{ color:#8D6E00; }
        @media (max-width: 1199.98px){
            .semaforo-card .chip-row{ flex-wrap:wrap; white-space:normal; }
        }

        /* ---- Bordes laterales por color ---- */
        .border-left-rojo     { border-left:.25rem solid #FFC7C7!important; }
        .border-left-naranja  { border-left:.25rem solid #FFD6A5!important; }
        .border-left-amarillo { border-left:.25rem solid #FFECB3!important; }

        /* ---- Tablas por color ---- */
        .color-list-card .card-body{ min-height:240px; }
        .color-table .table { font-size:13px; margin-bottom:0; }
        .color-table .table thead th{
            border-top:none; border-bottom:1px solid #eee;
            font-weight:700; color:#444;
        }
        .color-table .table tbody td{
            padding:6px 8px; vertical-align:middle;
            border-top:none; border-bottom:1px dashed #f0f0f0;
        }
        .color-table .table tbody tr:nth-child(odd){ background:#fcfcfc; }
        .color-table .table tbody tr:hover{ background:#f7f7f7; }

        /* Ellipsis */
        .td-ellipsis{ max-width:260px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
        .td-ellipsis.small{ max-width:200px; }

        /* Anchos sugeridos */
        .col-folio{ width:90px; }
        .col-fecha{ width:120px; text-align:right; color:#333; font-weight:600; }
    </style>

    <!-- Fallback inmediato: evita ReferenceError si algún script corre antes del inject -->
    <script type="text/javascript">
        if (typeof window.estadisticaData === "undefined") window.estadisticaData = [];
        if (typeof window.semaforoDetalleData === "undefined") window.semaforoDetalleData = [];
    </script>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="cphContenido" runat="server">

    <!-- Timer: refresca cada 5s -->
    <asp:Timer ID="tmrRefresh" runat="server" Interval="5000" OnTick="tmrRefresh_Tick" EnableViewState="false" />

    <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional" ChildrenAsTriggers="false">
        <ContentTemplate>
            <!-- HF expone TipoOrg a JS -->
            <asp:HiddenField ID="hfTipoOrg" runat="server" ClientIDMode="Static" />

            <div class="container-fluid">
                <!-- Tarjetas de Totales -->
                <div class="row dashboard-cards">
                    <div class="col-xl-2 col-md-4 mb-4">
                        <div class="card border-left-primary shadow h-100 py-2 text-center metric-card">
                            <div class="card-body">
                                <div class="text-xs font-weight-bold text-primary text-uppercase mb-1">Oficios Totales</div>
                                <asp:Label ID="lblTotalOficiosUnicos" runat="server" CssClass="display-4" Text="0"></asp:Label>
                            </div>
                        </div>
                    </div>

                    <div class="col-xl-2 col-md-4 mb-4">
                        <div class="card border-left-primary shadow h-100 py-2 text-center metric-card">
                            <div class="card-body">
                                <div class="text-xs font-weight-bold text-primary text-uppercase mb-1">Por Turnar</div>
                                <asp:Label ID="lblTotalCapturados" runat="server" CssClass="display-4" Text="0" ClientIDMode="Static"></asp:Label>
                            </div>
                        </div>
                    </div>

                    <div class="col-xl-2 col-md-4 mb-4">
                        <div class="card border-left-success shadow h-100 py-2 text-center metric-card">
                            <div class="card-body">
                                <div class="text-xs font-weight-bold text-success text-uppercase mb-1">Turnados</div>
                                <asp:Label ID="lblTotalTurnados" runat="server" CssClass="display-4" Text="0" ClientIDMode="Static"></asp:Label>
                            </div>
                        </div>
                    </div>

                    <div class="col-xl-2 col-md-4 mb-4">
                        <div class="card border-left-info shadow h-100 py-2 text-center metric-card">
                            <div class="card-body">
                                <div class="text-xs font-weight-bold text-info text-uppercase mb-1">En Proceso</div>
                                <asp:Label ID="lblTotalEnProceso" runat="server" CssClass="display-4" Text="0" ClientIDMode="Static"></asp:Label>
                            </div>
                        </div>
                    </div>

                    <div class="col-xl-2 col-md-4 mb-4">
                        <div class="card border-left-dark shadow h-100 py-2 text-center metric-card">
                            <div class="card-body">
                                <div class="text-xs font-weight-bold text-dark text-uppercase mb-1">Concluidos</div>
                                <asp:Label ID="lbltotalConcluidos" runat="server" CssClass="display-4" Text="0" ClientIDMode="Static"></asp:Label>
                            </div>
                        </div>
                    </div>

                    <!-- Semáforo total -->
                    <div class="col-xl-2 col-md-4 mb-4">
                        <div class="card border-left-warning shadow h-100 py-2 text-center metric-card semaforo-card">
                            <div class="card-body">
                                <div class="text-xs font-weight-bold text-warning text-uppercase mb-1">Oficios con Fecha de Atención</div>
                                <asp:Label ID="lblSemaforoTotal" runat="server" CssClass="display-4" Text="0"></asp:Label>

                                <div class="chip-row">
                                    <span class="chip rojo">Vencidos: <span class="value"><asp:Label ID="lblSemaforoRojo" runat="server" Text="0" /></span></span>
                                    <span class="chip naranja">Por vencer: <span class="value"><asp:Label ID="lblSemaforoNaranja" runat="server" Text="0" /></span></span>
                                    <span class="chip amarillo">A tiempo: <span class="value"><asp:Label ID="lblSemaforoAmarillo" runat="server" Text="0" /></span></span>
                                </div>

                                <asp:Label ID="lblSemaforoError" runat="server" Visible="false" ForeColor="Red" Style="display:block;margin-top:6px;"></asp:Label>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Gráfica + Barras -->
                <div class="row">
                    <div class="col-xl-4 col-lg-5 mb-4">
                        <div class="card shadow">
                            <div class="card-header py-3 d-flex flex-row align-items-center justify-content-between">
                                <h6 class="m-0 font-weight-bold text-primary">Total de Registros por Estatus</h6>
                            </div>
                            <div class="card-body">
                                <div class="chart-pie pt-4 pb-2">
                                    <canvas id="myPieChart"></canvas>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Dirección -->
                    <div class="col-xl-4 col-md-6 mb-4" id="cardDireccionContainer">
                        <div class="card shadow h-100 py-2">
                            <div class="card-header py-3">
                                <h6 class="m-0 font-weight-bold text-primary">Oficios por Dirección</h6>
                            </div>
                            <div class="card-body scrollable-card-body" id="direccionBars"></div>
                        </div>
                    </div>

                    <!-- Departamento -->
                    <div class="col-xl-4 col-md-6 mb-4" id="cardDepartamentoContainer">
                        <div class="card shadow h-100 py-2">
                            <div class="card-header py-3">
                                <h6 class="m-0 font-weight-bold text-success">Oficios por Departamento</h6>
                            </div>
                            <div class="card-body scrollable-card-body" id="departamentoBars"></div>
                        </div>
                    </div>

                    <!-- Área -->
                    <div class="col-xl-4 col-md-6 mb-4" id="cardAreaContainer">
                        <div class="card shadow h-100 py-2">
                            <div class="card-header py-3">
                                <h6 class="m-0 font-weight-bold text-info">Oficios por Área</h6>
                            </div>
                            <div class="card-body scrollable-card-body" id="areaBars"></div>
                        </div>
                    </div>
                </div>

                <!-- Listas por color -->
                <div class="row">
                    <!-- ROJO -->
                    <div class="col-xl-4 col-md-6 mb-4" id="cardRojo">
                        <div class="card shadow h-100 py-2 color-list-card border-left-rojo">
                            <div class="card-header py-3">
                                <h6 class="m-0 font-weight-bold" style="color:#B71C1C;">Oficios VENCIDOS</h6>
                            </div>
                            <div class="card-body scrollable-card-body color-table">
                                <div class="table-responsive">
                                    <table class="table table-sm table-borderless">
                                        <thead>
                                            <tr>
                                                <th class="col-folio">Folio</th>
                                                <th class="small">No. Oficio</th>
                                                <th class="th-entidad">Dirección</th>
                                                <th class="col-fecha">Fecha Máxima</th>
                                            </tr>
                                        </thead>
                                        <tbody id="tblRojoBody"></tbody>
                                    </table>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- NARANJA -->
                    <div class="col-xl-4 col-md-6 mb-4" id="cardNaranja">
                        <div class="card shadow h-100 py-2 color-list-card border-left-naranja">
                            <div class="card-header py-3">
                                <h6 class="m-0 font-weight-bold" style="color:#E65100;">Oficios por vencer</h6>
                            </div>
                            <div class="card-body scrollable-card-body color-table">
                                <div class="table-responsive">
                                    <table class="table table-sm table-borderless">
                                        <thead>
                                            <tr>
                                                <th class="col-folio">Folio</th>
                                                <th class="small">No. Oficio</th>
                                                <th class="th-entidad">Dirección</th>
                                                <th class="col-fecha">Fecha Máxima</th>
                                            </tr>
                                        </thead>
                                        <tbody id="tblNaranjaBody"></tbody>
                                    </table>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- AMARILLO -->
                    <div class="col-xl-4 col-md-6 mb-4" id="cardAmarillo">
                        <div class="card shadow h-100 py-2 color-list-card border-left-amarillo">
                            <div class="card-header py-3">
                                <h6 class="m-0 font-weight-bold" style="color:#B08900;">Oficios a Tiempo</h6>
                            </div>
                            <div class="card-body scrollable-card-body color-table">
                                <div class="table-responsive">
                                    <table class="table table-sm table-borderless">
                                        <thead>
                                            <tr>
                                                <th class="col-folio">Folio</th>
                                                <th class="small">No. Oficio</th>
                                                <th class="th-entidad">Dirección</th>
                                                <th class="col-fecha">Fecha Máxima</th>
                                            </tr>
                                        </thead>
                                        <tbody id="tblAmarilloBody"></tbody>
                                    </table>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- IMPORTANTE: NO declares aquí var estadisticaData = ...  -->
                <!-- Los datos llegan por ScriptManager desde el code-behind a window.* -->

            </div>
        </ContentTemplate>
        <Triggers>
            <asp:AsyncPostBackTrigger ControlID="tmrRefresh" EventName="Tick" />
        </Triggers>
    </asp:UpdatePanel>

    <!-- Chart.js -->
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>

    <script type="text/javascript">
        /* ========= Utilidades ========= */
        function normalizeText(s) {
            return (s || '').toString().normalize('NFD').replace(/[\u0300-\u036f]/g, '').trim().toLowerCase();
        }
        function formatDateDMY(v) {
            if (!v) return '';
            const s = v.toString();
            const iso = s.length >= 10 ? s.substring(0, 10) : s;
            const y = iso.substring(0, 4), m = iso.substring(5, 7), d = iso.substring(8, 10);
            if (y && m && d && y !== '0001') return `${d}/${m}/${y}`;
            return '';
        }
        function getEntidadMapping() {
            const tipoOrg = parseInt(document.getElementById('hfTipoOrg')?.value || '1', 10);
            if (tipoOrg === 3) return { label: 'Departamento', key: 'Quien_Departamento' };
            if (tipoOrg === 4 || tipoOrg === 5) return { label: 'Área', key: 'Quien_Area' };
            return { label: 'Dirección', key: 'Quien_Direccion' };
        }
        function setEntidadHeaders() {
            const map = getEntidadMapping();
            document.querySelectorAll('.color-list-card .th-entidad').forEach(th => th.textContent = map.label);
        }

        /* ========= Barras ========= */
        function renderBarsWithSelectedEstatus(data, containerId, cardContainerId, tipo) {
            const card = document.getElementById(cardContainerId);
            let filteredData = (data || []).filter(x =>
                x[tipo] && x[tipo].toLowerCase() !== "null" && !x[tipo].toLowerCase().startsWith("sin")
            );
            if (!filteredData || filteredData.length === 0) {
                if (card) card.style.display = "none";
                const cont = document.getElementById(containerId);
                if (cont) cont.innerHTML = "";
                return;
            } else { if (card) card.style.display = ""; }

            let grouped = {};
            filteredData.forEach(item => {
                let key = item[tipo];
                if (!grouped[key]) grouped[key] = [];
                grouped[key].push(item);
            });

            let totalOficiosGrupo = 0;
            Object.keys(grouped).forEach(nombre => {
                totalOficiosGrupo += grouped[nombre][0].OficiosTotales || 0;
            });

            let html = '';
            Object.keys(grouped).forEach((nombre) => {
                let items = grouped[nombre];
                let first = items[0];
                let cantidad = first.OficiosTotales || 0;
                let porcentaje = totalOficiosGrupo > 0 ? ((cantidad / totalOficiosGrupo) * 100).toFixed(1) : 0;
                html += `
                    <div class="progress-bar-label" style="margin-bottom:4px;">
                        <span style="font-weight:600;font-size:15px;">${nombre}</span>
                        <span class="estatus-resumen" style="gap:10px;">
                            <span style="background:#e0f7fa;color:#17a2b8;border-radius:8px;padding:2px 10px;font-weight:600;">
                                Turnados: <span style="color:#17a673;">${first.Turnado ?? 0}</span>
                            </span>
                            <span style="background:#e8f5e9;color:#388e3c;border-radius:8px;padding:2px 10px;font-weight:600;">
                                Concluidos: <span style="color:#388e3c;">${first.Concluido ?? 0}</span>
                            </span>
                            <span style="background:#fff8e1;color:#ff9800;border-radius:8px;padding:2px 10px;font-weight:600;">
                                Conocimiento: <span style="color:#ff9800;">${first.ParaConocimiento ?? 0}</span>
                            </span>
                            <span style="background:#ede7f6;color:#6a1b9a;border-radius:8px;padding:2px 10px;font-weight:600;">
                                Recibido: <span style="color:#4a148c;">${first.Recibido ?? 0}</span>
                            </span>
                            <span style="background:#e0f2f1;color:#00695c;border-radius:8px;padding:2px 10px;font-weight:600;">
                                En Proceso: <span style="color:#004d40;">${first["En Proceso"] ?? 0}</span>
                            </span>
                        </span>
                    </div>
                    <div class="progress mb-2" style="background:linear-gradient(90deg,#e3e3e3 75%,#fff 100%);">
                        <div class="progress-bar bg-primary" style="width:${porcentaje}%">
                            <span class="oficios-on-bar">${porcentaje}%</span>
                        </div>
                    </div>
                `;
            });

            const cont = document.getElementById(containerId);
            if (cont) cont.innerHTML = html;
        }

        /* ========= Tablas por color ========= */
        function renderSemaforoColorTable(data, colorName, tbodyId, cardId) {
            const normColor = normalizeText(colorName);
            const map = getEntidadMapping();
            const rows = (data || []).filter(r => normalizeText(r.Semaforo) === normColor);

            const tbody = document.getElementById(tbodyId);
            const card = document.getElementById(cardId);
            if (!tbody || !card) return;

            if (!rows.length) {
                card.style.display = "none";
                tbody.innerHTML = "";
                return;
            } else {
                card.style.display = "";
            }

            rows.sort((a, b) => {
                const da = a.FechaMaximaAtencion_Direccion || a.CreatedDate || '';
                const db = b.FechaMaximaAtencion_Direccion || b.CreatedDate || '';
                return (da > db) ? 1 : (da < db) ? -1 : 0;
            });

            let html = '';
            rows.forEach(r => {
                const folio = r.Folio ?? '';
                const noOficio = r.NoOficio ?? '';
                const entidad = r[map.key] || '';
                const fmax = formatDateDMY(r.FechaMaximaAtencion_Direccion);
                html += `
                    <tr>
                        <td class="col-folio">${folio}</td>
                        <td class="td-ellipsis small" title="${noOficio}">${noOficio}</td>
                        <td class="td-ellipsis" title="${entidad}">${entidad}</td>
                        <td class="col-fecha">${fmax}</td>
                    </tr>`;
            });

            tbody.innerHTML = html;
        }

        /* ========= Gráfica PIE ========= */
        var _pieChart = null;
        function renderOrUpdatePie() {
            var capturados = parseInt(document.getElementById("lblTotalCapturados").innerText) || 0;
            var turnados = parseInt(document.getElementById("lblTotalTurnados").innerText) || 0;
            var enProceso = parseInt(document.getElementById("lblTotalEnProceso").innerText) || 0;

            var canvas = document.getElementById("myPieChart");
            if (!canvas) return;

            if (_pieChart) {
                try {
                    if (_pieChart.canvas !== canvas || !document.body.contains(_pieChart.canvas)) {
                        _pieChart.destroy();
                        _pieChart = null;
                    }
                } catch (e) { _pieChart = null; }
            }

            if (!_pieChart) {
                _pieChart = new Chart(canvas, {
                    type: 'doughnut',
                    data: {
                        labels: ["Por Turnar", "Turnados", "En Proceso"],
                        datasets: [{
                            data: [capturados, turnados, enProceso],
                            backgroundColor: ['#4e73df', '#1cc88a', '#36b9cc'],
                            hoverBackgroundColor: ['#2e59d9', '#17a673', '#2c9faf'],
                            hoverBorderColor: "rgba(234, 236, 244, 1)"
                        }]
                    },
                    options: { maintainAspectRatio: false, legend: { display: false }, cutoutPercentage: 80 }
                });
            } else {
                _pieChart.data.datasets[0].data = [capturados, turnados, enProceso];
                _pieChart.update();
            }
        }

        /* ========= Init ========= */
        function initDashboard() {
            // Fallback por si todavía no llegaron los injects (primer ciclo)
            if (typeof window.estadisticaData === "undefined") window.estadisticaData = [];
            if (typeof window.semaforoDetalleData === "undefined") window.semaforoDetalleData = [];

            setEntidadHeaders();

            const stats = window.estadisticaData || [];
            const semaf = window.semaforoDetalleData || [];

            renderBarsWithSelectedEstatus(stats, "direccionBars", "cardDireccionContainer", "Direccion");
            renderBarsWithSelectedEstatus(stats, "departamentoBars", "cardDepartamentoContainer", "Departamento");
            renderBarsWithSelectedEstatus(stats, "areaBars", "cardAreaContainer", "Area");

            renderSemaforoColorTable(semaf, "Rojo", "tblRojoBody", "cardRojo");
            renderSemaforoColorTable(semaf, "Naranja", "tblNaranjaBody", "cardNaranja");
            renderSemaforoColorTable(semaf, "Amarillo", "tblAmarilloBody", "cardAmarillo");

            renderOrUpdatePie();
        }

        // 1) Primera carga
        document.addEventListener("DOMContentLoaded", initDashboard);
        // 2) Re-ejecución tras cada postback parcial del UpdatePanel
        if (typeof (Sys) !== "undefined" && Sys.Application) {
            Sys.Application.add_load(initDashboard);
        }
    </script>
</asp:Content>
