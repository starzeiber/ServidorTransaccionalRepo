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
-- Author:		Uriel Martinez
-- Create date: 01/09/21
-- Description:	Consulta en busca de una transacción
-- =============================================
ALTER PROCEDURE sprObtenerInfoTrx
	@idGrupo int,
	@idCadena int,
	@idTienda int,
	@telefono varchar(10),
	@fecha varchar(10),
	@hora varchar(10),
	@numeroTransaccion int,
	@sku varchar(15)
AS
BEGIN
	SELECT RESPONSE_CODE AS codigoRespuesta, [AUTHORIZATION] AS autorizacion  
	FROM TransactionSales
	WHERE ID_GROUP=@idGrupo
	AND ID_CHAIN=@idCadena
	AND ID_MERCHANT=@idTienda
	AND NRO_TELEFONICO=@telefono
	AND LOCAL_DATE=@fecha
	AND LOCAL_TIME=@hora
	AND SYSTEMTRACE=@numeroTransaccion
	AND SKU=@sku
	AND STATUS=1
END
GO
