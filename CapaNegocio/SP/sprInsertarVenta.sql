-- ================================================
-- Template generated from Template Explorer using:
-- Create Procedure (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the procedure.
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Uriel Mart�nez
-- Create date: 04/08/2021
-- Description:	Inserta un registro en la tabla de ventas
-- =============================================
ALTER PROCEDURE sprInsertarVenta
	@fechaHora datetime,
	@fecha varchar(30),
	@hora varchar(30),
	@encabezado int,
	@pCode int,
	@issuer varchar(20),
	@sku varchar(20),
	@folio varchar(50),
	@numeroReferencia varchar(20),
	@telefono varchar(20),
	@monto decimal,	
	@idGrupo int,
	@idCadena int,
	@idTienda int,
	@idProveedor int,
	@idMaster int,
	@numTransaccion int,
	@autorizacion int,
	@codigoRespuesta int,
	@saldoActual int,
	@mensaje200 text,
	@mensaje210 text,
	@mensaje220 text,
	@mensaje230 text,
	@tipoTrx int
AS
BEGIN
	IF EXISTS (SELECT * FROM TransactionSales WHERE ID_GROUP=@idGrupo AND ID_CHAIN=@idCadena AND ID_MERCHANT=@idTienda AND LOCAL_DATE=@fecha AND SYSTEMTRACE=@numTransaccion)
	BEGIN
		UPDATE TransactionSales 
		SET RESPONSE_CODE=@codigoRespuesta,
		[AUTHORIZATION]=@autorizacion,
		MSG220=@mensaje220,
		MSG230=@mensaje230
		SELECT @@ROWCOUNT AS filas	
	END

	INSERT INTO TransactionSales
	(DATE_TIME,LOCAL_DATE,LOCAL_TIME,MSG_TYPE,PCODE,ISSUER,SKU,FOLIO,REFERENCE_NUMBER,NRO_TELEFONICO,AMOUNT,ID_GROUP,ID_CHAIN,ID_MERCHANT,ID_POS,ID_TELLER,ID_PROVIDER,ID_MASTER,SYSTEMTRACE,[AUTHORIZATION],RESPONSE_CODE,SALDOACTUAL,STATUS,STATUSPLAT,MSG200,MSG210,TypeTRX) values
	(@fechaHora,@fecha,@hora,@encabezado,@pCode,@issuer,@sku,@folio,@numeroReferencia,@telefono,@monto,@idGrupo,@idCadena,@idTienda,1,0,@idProveedor,@idMaster,@numTransaccion,@autorizacion,@codigoRespuesta,@saldoActual,1,0,@mensaje200,@mensaje210,@tipoTrx)
	SELECT @@ROWCOUNT AS filas	
END
GO
