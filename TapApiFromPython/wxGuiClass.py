# -*- coding: utf-8 -*- 

###########################################################################
## Python code generated with wxFormBuilder (version Jun 17 2015)
## http://www.wxformbuilder.org/
##
## PLEASE DO "NOT" EDIT THIS FILE!
###########################################################################

import wx
import wx.xrc

###########################################################################
## Class MainFrame
###########################################################################

class MainFrame ( wx.Frame ):
	
	def __init__( self, parent ):
		wx.Frame.__init__ ( self, parent, id = wx.ID_ANY, title = u"Python UI for TAP", pos = wx.DefaultPosition, size = wx.Size( 626,448 ), style = wx.DEFAULT_FRAME_STYLE|wx.TAB_TRAVERSAL )
		
		self.SetSizeHintsSz( wx.DefaultSize, wx.DefaultSize )
		
		BoxSizer = wx.BoxSizer( wx.VERTICAL )
		
		self.Ctrl_BrowseForTestPlanFile = wx.FilePickerCtrl( self, wx.ID_ANY, wx.EmptyString, u"Select a TestPlan file", u"*.TapPlan", wx.DefaultPosition, wx.DefaultSize, wx.FLP_DEFAULT_STYLE )
		BoxSizer.Add( self.Ctrl_BrowseForTestPlanFile, 0, wx.ALL|wx.EXPAND, 5 )
		
		self.Btn_RunPlan = wx.Button( self, wx.ID_ANY, u"Run Test Plan", wx.DefaultPosition, wx.DefaultSize, 0 )
		BoxSizer.Add( self.Btn_RunPlan, 0, wx.ALL|wx.ALIGN_RIGHT, 5 )
		
		listBox_LogPanelChoices = []
		self.listBox_LogPanel = wx.ListBox( self, wx.ID_ANY, wx.DefaultPosition, wx.DefaultSize, listBox_LogPanelChoices, 0 )
		BoxSizer.Add( self.listBox_LogPanel, 1, wx.ALL|wx.EXPAND, 5 )
		
		
		self.SetSizer( BoxSizer )
		self.Layout()
		self.m_statusBar1 = self.CreateStatusBar( 1, wx.ST_SIZEGRIP, wx.ID_ANY )
		
		self.Centre( wx.BOTH )
		
		# Connect Events
		self.Btn_RunPlan.Bind( wx.EVT_BUTTON, self.RunTestPlan_Clicked )
	
	def __del__( self ):
		pass
	
	
	# Virtual event handlers, overide them in your derived class
	def RunTestPlan_Clicked( self, event ):
		event.Skip()
	

