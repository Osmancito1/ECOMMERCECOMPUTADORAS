<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Pedido.aspx.cs" Inherits="EcommerceComputadorasNW.Pedido" %>

<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Finalizar Pedido - TechStore</title>
    <link rel="stylesheet" href="Content/pedidos.css">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet">
</head>
<body>
    <form id="form1" runat="server">
        <!-- Header -->
        <header class="header">
            <div class="container">
                <div class="header-content">
                    <div class="logo">
                        <a href="Default.aspx">
                            <i class="fas fa-laptop"></i>
                            <span>TechStore</span>
                        </a>
                    </div>
                    
                    <div class="checkout-progress">
                        <div class="progress-step active">
                            <div class="step-number">1</div>
                            <span>Información</span>
                        </div>
                        <div class="progress-line"></div>
                        <div class="progress-step">
                            <div class="step-number">2</div>
                            <span>Pago</span>
                        </div>
                        <div class="progress-line"></div>
                        <div class="progress-step">
                            <div class="step-number">3</div>
                            <span>Confirmación</span>
                        </div>
                    </div>

                    <div class="header-actions">
                        <a href="Cuenta.aspx" class="user-link">
                            <i class="fas fa-user"></i>
                            <span>Mi Cuenta</span>
                        </a>
                    </div>
                </div>
            </div>
        </header>

        <!-- Main Content -->
        <main class="main-content">
            <div class="container">
                <div class="checkout-layout">
                    <!-- Checkout Form -->
                    <div class="checkout-form-section">
                        <div class="section-header">
                            <h1>Información del Pedido</h1>
                            <p>Completa los datos para procesar tu compra</p>
                        </div>

                        <div class="checkout-form">
                            <!-- Contact Information -->
                            <div class="form-section">
                                <div class="section-title">
                                    <i class="fas fa-user"></i>
                                    <h3>Información de Contacto</h3>
                                </div>
                                
                                <div class="form-row">
                                    <div class="form-group">
                                        <label for="txtFirstName">Nombre *</label>
                                        <asp:TextBox ID="txtFirstName" runat="server" CssClass="form-control" required></asp:TextBox>
                                        <div class="input-focus-effect"></div>
                                    </div>
                                    <div class="form-group">
                                        <label for="txtLastName">Apellido *</label>
                                        <asp:TextBox ID="txtLastName" runat="server" CssClass="form-control" required></asp:TextBox>
                                        <div class="input-focus-effect"></div>
                                    </div>
                                </div>

                                <div class="form-row">
                                    <div class="form-group">
                                        <label for="txtEmail">Email *</label>
                                        <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="form-control" required></asp:TextBox>
                                        <div class="input-focus-effect"></div>
                                    </div>
                                    <div class="form-group">
                                        <label for="txtPhone">Teléfono *</label>
                                        <asp:TextBox ID="txtPhone" runat="server" TextMode="Phone" CssClass="form-control" required></asp:TextBox>
                                        <div class="input-focus-effect"></div>
                                    </div>
                                </div>
                            </div>

                            <!-- Shipping Address -->
                            <div class="form-section">
                                <div class="section-title">
                                    <i class="fas fa-truck"></i>
                                    <h3>Dirección de Envío</h3>
                                </div>

                                <div class="form-group">
                                    <label for="txtAddress">Dirección *</label>
                                    <asp:TextBox ID="txtAddress" runat="server" CssClass="form-control" placeholder="Calle, número, colonia" required></asp:TextBox>
                                    <div class="input-focus-effect"></div>
                                </div>

                                <div class="form-row">
                                    <div class="form-group">
                                        <label for="txtCity">Ciudad *</label>
                                        <asp:TextBox ID="txtCity" runat="server" CssClass="form-control" required></asp:TextBox>
                                        <div class="input-focus-effect"></div>
                                    </div>
                                    <div class="form-group">
                                        <label for="ddlState">Estado *</label>
                                        <asp:DropDownList ID="ddlState" runat="server" CssClass="form-control" required>
                                            <asp:ListItem Value="">Seleccionar estado</asp:ListItem>
                                            <asp:ListItem Value="CDMX">Ciudad de México</asp:ListItem>
                                            <asp:ListItem Value="JAL">Jalisco</asp:ListItem>
                                            <asp:ListItem Value="NL">Nuevo León</asp:ListItem>
                                            <asp:ListItem Value="BC">Baja California</asp:ListItem>
                                            <asp:ListItem Value="SON">Sonora</asp:ListItem>
                                        </asp:DropDownList>
                                        <div class="input-focus-effect"></div>
                                    </div>
                                    <div class="form-group">
                                        <label for="txtZipCode">Código Postal *</label>
                                        <asp:TextBox ID="txtZipCode" runat="server" CssClass="form-control" required></asp:TextBox>
                                        <div class="input-focus-effect"></div>
                                    </div>
                                </div>

                                <div class="form-group">
                                    <label for="ddlCountry">País *</label>
                                    <asp:DropDownList ID="ddlCountry" runat="server" CssClass="form-control" required>
                                        <asp:ListItem Value="MX">México</asp:ListItem>
                                        <asp:ListItem Value="US">Estados Unidos</asp:ListItem>
                                        <asp:ListItem Value="CA">Canadá</asp:ListItem>
                                    </asp:DropDownList>
                                    <div class="input-focus-effect"></div>
                                </div>
                            </div>

                            <!-- Order Summary -->
                            <div class="order-summary-section">
                                <div class="order-summary">
                                    <h3>Resumen del Pedido</h3>

                                    <div class="order-items">
                                        <asp:Repeater ID="rptProductos" runat="server">
                                            <ItemTemplate>
                                                <div class="order-item">
                                                    <div class="item-info">
                                                        <h4><%# Eval("NomPro") %></h4>
                                                        <div class="item-details">
                                                            <span><%# Eval("CantPro") %> x <%# Eval("PrecUni", "{0:C}") %></span>
                                                            <span class="item-subtotal"><%# Eval("Subtotal", "{0:C}") %></span>
                                                        </div>
                                                    </div>
                                                </div>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>

                                    <div class="order-totals">
                                        <div class="total-row">
                                            <span>Subtotal</span>
                                            <span class="total-value"><asp:Literal ID="lblSubtotal" runat="server" Text="$0.00" /></span>
                                        </div>
                                        <div class="total-row">
                                            <span>Envío</span>
                                            <span class="total-value"><asp:Literal ID="lblEnvio" runat="server" Text="$0.00" /></span>
                                        </div>
                                        <div class="total-row">
                                            <span>Impuestos</span>
                                            <span class="total-value"><asp:Literal ID="lblImpuestos" runat="server" Text="$0.00" /></span>
                                        </div>
                                        <div class="total-row grand-total">
                                            <span>Total</span>
                                            <span class="total-value"><asp:Literal ID="lblTotal" runat="server" Text="$0.00" /></span>
                                        </div>
                                    </div>

                                    <div class="promo-code">
                                        <div class="promo-input">
                                            <asp:TextBox ID="txtPromoCode" runat="server" CssClass="promo-textbox" placeholder="Código de descuento"></asp:TextBox>
                                            <asp:Button ID="btnAplicarPromo" runat="server" CssClass="promo-button" Text="Aplicar" OnClick="btnAplicarPromo_Click" />
                                        </div>
                                        <asp:Label ID="lblMensajePromo" runat="server" CssClass="promo-message" Visible="false"></asp:Label>
                                    </div>

                                    <div class="security-badges">
                                        <div class="security-badge">
                                            <i class="fas fa-shield-alt"></i>
                                            <span>Compra Segura</span>
                                        </div>
                                        <div class="security-badge">
                                            <i class="fas fa-truck"></i>
                                            <span>Envío Garantizado</span>
                                        </div>
                                        <div class="security-badge">
                                            <i class="fas fa-undo"></i>
                                            <span>30 días de devolución</span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <asp:Label ID="lblError" runat="server" Text="Label"></asp:Label>
                            <!-- Form Actions -->
                            <div class="form-actions">
                                <asp:Button ID="btnVolver" runat="server" CssClass="btn-secondary" Text="Volver al Carrito" OnClick="btnVolver_Click" />
                                <asp:Button ID="btnContinuar" runat="server" CssClass="btn-primary" Text="Continuar al Pago" OnClick="btnContinuar_Click" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </main>
    </form>

    <script src="Scripts/pedido.js"></script>
</body>
</html>