USE [Crawler]
GO
/****** Object:  Table [dbo].[Session]    Script Date: 09/10/2008 12:15:47 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Session](
	[SessionKey] [uniqueidentifier] NOT NULL,
	[ScanDate] [datetime] NOT NULL CONSTRAINT [DF_Session_ScanDate]  DEFAULT (getutcdate()),
	[Url] [varchar](255) NOT NULL,
 CONSTRAINT [PK_Session] PRIMARY KEY CLUSTERED 
(
	[SessionKey] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SessionScanRelation]    Script Date: 09/10/2008 12:15:55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SessionScanRelation](
	[SessionKey] [uniqueidentifier] NOT NULL,
	[UrlHash] [char](40) NOT NULL,
	[RelatedHash] [char](40) NOT NULL,
	[Related] [varchar](255) NOT NULL,
	[Count] [int] NOT NULL CONSTRAINT [DF_SessionScanRelation_Count]  DEFAULT ((1)),
 CONSTRAINT [PK_SessionScanRelation] PRIMARY KEY CLUSTERED 
(
	[SessionKey] ASC,
	[UrlHash] ASC,
	[RelatedHash] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SessionScan]    Script Date: 09/10/2008 12:15:53 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SessionScan](
	[SessionKey] [uniqueidentifier] NOT NULL,
	[UrlHash] [char](40) NOT NULL,
	[ScanDate] [datetime] NOT NULL CONSTRAINT [DF_SessionScan_DateTime]  DEFAULT (getutcdate()),
	[ContentHash] [char](40) NULL,
	[Host] [varchar](255) NULL,
	[Base] [varchar](255) NULL,
	[Found] [varchar](255) NULL,
	[Url] [varchar](255) NULL,
	[Redirect] [varchar](255) NULL,
	[Method] [varchar](10) NULL,
	[Status] [int] NULL,
	[Title] [varchar](max) NULL,
	[Description] [varchar](max) NULL,
	[Keywords] [varchar](max) NULL,
	[Robots] [varchar](max) NULL,
	[ContentType] [varchar](50) NULL,
	[ContentEncoding] [varchar](50) NULL,
	[ContentLength] [bigint] NULL,
	[CacheControl] [varchar](50) NULL,
	[Expires] [varchar](50) NULL,
 CONSTRAINT [PK_SessionScan] PRIMARY KEY NONCLUSTERED 
(
	[SessionKey] ASC,
	[UrlHash] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  ForeignKey [FK_SessionScan_Session]    Script Date: 09/10/2008 12:15:53 ******/
ALTER TABLE [dbo].[SessionScan]  WITH CHECK ADD  CONSTRAINT [FK_SessionScan_Session] FOREIGN KEY([SessionKey])
REFERENCES [dbo].[Session] ([SessionKey])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SessionScan] CHECK CONSTRAINT [FK_SessionScan_Session]
GO
/****** Object:  ForeignKey [FK_SessionScanRelation_SessionScan]    Script Date: 09/10/2008 12:15:55 ******/
ALTER TABLE [dbo].[SessionScanRelation]  WITH CHECK ADD  CONSTRAINT [FK_SessionScanRelation_SessionScan] FOREIGN KEY([SessionKey], [UrlHash])
REFERENCES [dbo].[SessionScan] ([SessionKey], [UrlHash])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SessionScanRelation] CHECK CONSTRAINT [FK_SessionScanRelation_SessionScan]
GO
