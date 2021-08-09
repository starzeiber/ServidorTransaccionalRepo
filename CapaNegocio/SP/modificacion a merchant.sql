USE [TEN_ ADMINSERVICIOS]
GO

/****** Object:  Table [dbo].[MERCHANT]    Script Date: 04/08/2021 12:08:02 p.m. ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER TABLE [dbo].[MERCHANT]	
	add [controlCredito] [int] NOT NULL 
GO

ALTER TABLE [dbo].[MERCHANT] ADD  CONSTRAINT [DF_MERCHANT_controlCredito]  DEFAULT ((0)) FOR [controlCredito]
GO


