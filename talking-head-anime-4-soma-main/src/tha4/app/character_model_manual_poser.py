import logging
import os
import sys
import time
from typing import List

from tha4.charmodel.character_model import CharacterModel
from tha4.image_util import resize_PIL_image, convert_output_image_from_torch_to_numpy
from tha4.poser.modes.mode_14 import get_pose_parameters

sys.path.append(os.getcwd())

import PIL.Image
import torch
import wx

from tha4.poser.poser import PoseParameterCategory, PoseParameterGroup


class MorphCategoryControlPanel(wx.Panel):
    def __init__(self,
                 parent,
                 title: str,
                 pose_param_category: PoseParameterCategory,
                 param_groups: List[PoseParameterGroup]):
        super().__init__(parent, style=wx.SIMPLE_BORDER)
        self.pose_param_category = pose_param_category
        self.sizer = wx.BoxSizer(wx.VERTICAL)
        self.SetSizer(self.sizer)
        self.SetAutoLayout(1)

        title_text = wx.StaticText(self, label=title, style=wx.ALIGN_CENTER)
        self.sizer.Add(title_text, 0, wx.EXPAND)

        self.param_groups = [group for group in param_groups if group.get_category() == pose_param_category]
        self.choice = wx.Choice(self, choices=[group.get_group_name() for group in self.param_groups])
        if len(self.param_groups) > 0:
            self.choice.SetSelection(0)
        self.choice.Bind(wx.EVT_CHOICE, self.on_choice_updated)
        self.sizer.Add(self.choice, 0, wx.EXPAND)

        self.left_slider = wx.Slider(self, minValue=-1000, maxValue=1000, value=-1000, style=wx.HORIZONTAL)
        self.sizer.Add(self.left_slider, 0, wx.EXPAND)

        self.right_slider = wx.Slider(self, minValue=-1000, maxValue=1000, value=-1000, style=wx.HORIZONTAL)
        self.sizer.Add(self.right_slider, 0, wx.EXPAND)

        self.checkbox = wx.CheckBox(self, label="Show")
        self.checkbox.SetValue(True)
        self.sizer.Add(self.checkbox, 0, wx.SHAPED | wx.ALIGN_CENTER)

        self.update_ui()

        self.sizer.Fit(self)

    def update_ui(self):
        param_group = self.param_groups[self.choice.GetSelection()]
        if param_group.is_discrete():
            self.left_slider.Enable(False)
            self.right_slider.Enable(False)
            self.checkbox.Enable(True)
        elif param_group.get_arity() == 1:
            self.left_slider.Enable(True)
            self.right_slider.Enable(False)
            self.checkbox.Enable(False)
        else:
            self.left_slider.Enable(True)
            self.right_slider.Enable(True)
            self.checkbox.Enable(False)

    def on_choice_updated(self, event: wx.Event):
        param_group = self.param_groups[self.choice.GetSelection()]
        if param_group.is_discrete():
            self.checkbox.SetValue(True)
        self.update_ui()

    def set_param_value(self, pose: List[float]):
        if len(self.param_groups) == 0:
            return
        selected_morph_index = self.choice.GetSelection()
        param_group = self.param_groups[selected_morph_index]
        param_index = param_group.get_parameter_index()
        if param_group.is_discrete():
            if self.checkbox.GetValue():
                for i in range(param_group.get_arity()):
                    pose[param_index + i] = 1.0
        else:
            param_range = param_group.get_range()
            alpha = (self.left_slider.GetValue() + 1000) / 2000.0
            pose[param_index] = param_range[0] + (param_range[1] - param_range[0]) * alpha
            if param_group.get_arity() == 2:
                alpha = (self.right_slider.GetValue() + 1000) / 2000.0
                pose[param_index + 1] = param_range[0] + (param_range[1] - param_range[0]) * alpha


class SimpleParamGroupsControlPanel(wx.Panel):
    def __init__(self, parent,
                 pose_param_category: PoseParameterCategory,
                 param_groups: List[PoseParameterGroup]):
        super().__init__(parent, style=wx.SIMPLE_BORDER)
        self.sizer = wx.BoxSizer(wx.VERTICAL)
        self.SetSizer(self.sizer)
        self.SetAutoLayout(1)

        self.param_groups = [group for group in param_groups if group.get_category() == pose_param_category]
        for param_group in self.param_groups:
            assert not param_group.is_discrete()
            assert param_group.get_arity() == 1

        self.sliders = []
        for param_group in self.param_groups:
            static_text = wx.StaticText(
                self,
                label="   ------------ %s ------------   " % param_group.get_group_name(), style=wx.ALIGN_CENTER)
            self.sizer.Add(static_text, 0, wx.EXPAND)
            range = param_group.get_range()
            min_value = int(range[0] * 1000)
            max_value = int(range[1] * 1000)
            slider = wx.Slider(self, minValue=min_value, maxValue=max_value, value=0, style=wx.HORIZONTAL)
            self.sizer.Add(slider, 0, wx.EXPAND)
            self.sliders.append(slider)

        self.sizer.Fit(self)

    def set_param_value(self, pose: List[float]):
        if len(self.param_groups) == 0:
            return
        for param_group_index in range(len(self.param_groups)):
            param_group = self.param_groups[param_group_index]
            slider = self.sliders[param_group_index]
            param_range = param_group.get_range()
            param_index = param_group.get_parameter_index()
            alpha = (slider.GetValue() - slider.GetMin()) * 1.0 / (slider.GetMax() - slider.GetMin())
            pose[param_index] = param_range[0] + (param_range[1] - param_range[0]) * alpha


class MainFrame(wx.Frame):
    IMAGE_SIZE = 512
    OUTPUT_LENGTH = 6
    NUM_PARAMETERS = 45

    def __init__(self, device: torch.device):
        super().__init__(None, wx.ID_ANY, "Poser")
        self.poser = None
        self.device = device

        self.wx_source_image = None
        self.torch_source_image = None

        self.main_sizer = wx.BoxSizer(wx.HORIZONTAL)
        self.SetSizer(self.main_sizer)
        self.SetAutoLayout(1)
        self.init_left_panel()
        self.init_control_panel()
        self.init_right_panel()
        self.main_sizer.Fit(self)

        self.timer = wx.Timer(self, wx.ID_ANY)
        self.Bind(wx.EVT_TIMER, self.update_images, self.timer)

        save_image_id = wx.NewIdRef()
        self.Bind(wx.EVT_MENU, self.on_save_image, id=save_image_id)
        accelerator_table = wx.AcceleratorTable([
            (wx.ACCEL_CTRL, ord('S'), save_image_id)
        ])
        self.SetAcceleratorTable(accelerator_table)

        self.last_pose = None
        self.last_output_index = self.output_index_choice.GetSelection()
        self.last_output_numpy_image = None

        self.wx_source_image = None
        self.torch_source_image = None
        self.source_image_bitmap = wx.Bitmap(MainFrame.IMAGE_SIZE, MainFrame.IMAGE_SIZE)
        self.result_image_bitmap = wx.Bitmap(MainFrame.IMAGE_SIZE, MainFrame.IMAGE_SIZE)
        self.source_image_dirty = True

    def init_left_panel(self):
        self.control_panel = wx.Panel(self, style=wx.SIMPLE_BORDER, size=(MainFrame.IMAGE_SIZE, -1))
        self.left_panel = wx.Panel(self, style=wx.SIMPLE_BORDER)
        left_panel_sizer = wx.BoxSizer(wx.VERTICAL)
        self.left_panel.SetSizer(left_panel_sizer)
        self.left_panel.SetAutoLayout(1)

        self.source_image_panel = wx.Panel(self.left_panel, size=(MainFrame.IMAGE_SIZE, MainFrame.IMAGE_SIZE),
                                           style=wx.SIMPLE_BORDER)
        self.source_image_panel.Bind(wx.EVT_PAINT, self.paint_source_image_panel)
        self.source_image_panel.Bind(wx.EVT_ERASE_BACKGROUND, self.on_erase_background)
        left_panel_sizer.Add(self.source_image_panel, 0, wx.FIXED_MINSIZE)

        self.load_model_button = wx.Button(self.left_panel, wx.ID_ANY, "\nLoad Model\n\n")
        left_panel_sizer.Add(self.load_model_button, 1, wx.EXPAND)
        self.load_model_button.Bind(wx.EVT_BUTTON, self.load_model)

        left_panel_sizer.Fit(self.left_panel)
        self.main_sizer.Add(self.left_panel, 0, wx.FIXED_MINSIZE)

    def on_erase_background(self, event: wx.Event):
        pass

    def init_control_panel(self):
        self.control_panel_sizer = wx.BoxSizer(wx.VERTICAL)
        self.control_panel.SetSizer(self.control_panel_sizer)
        self.control_panel.SetMinSize(wx.Size(256, 1))

        morph_categories = [
            PoseParameterCategory.EYEBROW,
            PoseParameterCategory.EYE,
            PoseParameterCategory.MOUTH,
            PoseParameterCategory.IRIS_MORPH
        ]
        morph_category_titles = {
            PoseParameterCategory.EYEBROW: "   ------------ Eyebrow ------------   ",
            PoseParameterCategory.EYE: "   ------------ Eye ------------   ",
            PoseParameterCategory.MOUTH: "   ------------ Mouth ------------   ",
            PoseParameterCategory.IRIS_MORPH: "   ------------ Iris morphs ------------   ",
        }
        self.morph_control_panels = {}
        param_groups = get_pose_parameters().get_pose_parameter_groups()
        for category in morph_categories:
            filtered_param_groups = [group for group in param_groups if group.get_category() == category]
            if len(filtered_param_groups) == 0:
                continue
            control_panel = MorphCategoryControlPanel(
                self.control_panel,
                morph_category_titles[category],
                category,
                param_groups)
            self.morph_control_panels[category] = control_panel
            self.control_panel_sizer.Add(control_panel, 0, wx.EXPAND)

        self.non_morph_control_panels = {}
        non_morph_categories = [
            PoseParameterCategory.IRIS_ROTATION,
            PoseParameterCategory.FACE_ROTATION,
            PoseParameterCategory.BODY_ROTATION,
            PoseParameterCategory.BREATHING
        ]
        for category in non_morph_categories:
            filtered_param_groups = [group for group in param_groups if group.get_category() == category]
            if len(filtered_param_groups) == 0:
                continue
            control_panel = SimpleParamGroupsControlPanel(
                self.control_panel,
                category,
                param_groups)
            self.non_morph_control_panels[category] = control_panel
            self.control_panel_sizer.Add(control_panel, 0, wx.EXPAND)

        self.control_panel_sizer.Fit(self.control_panel)
        self.main_sizer.Add(self.control_panel, 1, wx.FIXED_MINSIZE)

    def init_right_panel(self):
        self.right_panel = wx.Panel(self, style=wx.SIMPLE_BORDER)
        right_panel_sizer = wx.BoxSizer(wx.VERTICAL)
        self.right_panel.SetSizer(right_panel_sizer)
        self.right_panel.SetAutoLayout(1)

        self.result_image_panel = wx.Panel(self.right_panel,
                                           size=(MainFrame.IMAGE_SIZE, MainFrame.IMAGE_SIZE),
                                           style=wx.SIMPLE_BORDER)
        self.result_image_panel.Bind(wx.EVT_PAINT, self.paint_result_image_panel)
        self.result_image_panel.Bind(wx.EVT_ERASE_BACKGROUND, self.on_erase_background)
        self.output_index_choice = wx.Choice(
            self.right_panel,
            choices=[str(i) for i in range(MainFrame.OUTPUT_LENGTH)])
        self.output_index_choice.SetSelection(0)
        right_panel_sizer.Add(self.result_image_panel, 0, wx.FIXED_MINSIZE)
        right_panel_sizer.Add(self.output_index_choice, 0, wx.EXPAND)

        self.save_image_button = wx.Button(self.right_panel, wx.ID_ANY, "\nSave Image\n\n")
        right_panel_sizer.Add(self.save_image_button, 1, wx.EXPAND)
        self.save_image_button.Bind(wx.EVT_BUTTON, self.on_save_image)

        right_panel_sizer.Fit(self.right_panel)
        self.main_sizer.Add(self.right_panel, 0, wx.FIXED_MINSIZE)

    def create_param_category_choice(self, param_category: PoseParameterCategory):
        params = []
        for param_group in self.poser.get_pose_parameter_groups():
            if param_group.get_category() == param_category:
                params.append(param_group.get_group_name())
        choice = wx.Choice(self.control_panel, choices=params)
        if len(params) > 0:
            choice.SetSelection(0)
        return choice

    def load_model(self, event: wx.Event):
        dir_name = "data/character_models"
        file_dialog = wx.FileDialog(self, "Choose a model", dir_name, "", "*.yaml", wx.FD_OPEN)
        if file_dialog.ShowModal() == wx.ID_OK:
            character_model_file_name = os.path.join(file_dialog.GetDirectory(), file_dialog.GetFilename())
            try:
                self.character_model = CharacterModel.load(character_model_file_name)
                self.torch_source_image = self.character_model.get_character_image(self.device)
                pil_image = resize_PIL_image(
                    PIL.Image.open(self.character_model.character_image_file_name),
                    (MainFrame.IMAGE_SIZE, MainFrame.IMAGE_SIZE))
                w, h = pil_image.size
                self.wx_source_image = wx.Bitmap.FromBufferRGBA(w, h, pil_image.convert("RGBA").tobytes())
                self.poser = self.character_model.get_poser(self.device)
                self.source_image_dirty = True
                self.Refresh()
                self.Update()
            except RuntimeError as e:
                message_dialog = wx.MessageDialog(
                    self, "Could not load character model " + character_model_file_name, "Poser", wx.OK)
                message_dialog.ShowModal()
                message_dialog.Destroy()
        file_dialog.Destroy()

    def paint_source_image_panel(self, event: wx.Event):
        wx.BufferedPaintDC(self.source_image_panel, self.source_image_bitmap)

    def paint_result_image_panel(self, event: wx.Event):
        wx.BufferedPaintDC(self.result_image_panel, self.result_image_bitmap)

    def draw_nothing_yet_string_to_bitmap(self, bitmap):
        dc = wx.MemoryDC()
        dc.SelectObject(bitmap)

        dc.Clear()
        font = wx.Font(wx.FontInfo(14).Family(wx.FONTFAMILY_SWISS))
        dc.SetFont(font)
        w, h = dc.GetTextExtent("Nothing yet!")
        dc.DrawText("Nothing yet!", (MainFrame.IMAGE_SIZE - w) // 2, (MainFrame.IMAGE_SIZE - - h) // 2)

        del dc

    def get_current_pose(self):
        current_pose = [0.0 for i in range(MainFrame.NUM_PARAMETERS)]
        for morph_control_panel in self.morph_control_panels.values():
            morph_control_panel.set_param_value(current_pose)
        for rotation_control_panel in self.non_morph_control_panels.values():
            rotation_control_panel.set_param_value(current_pose)
        return current_pose

    def update_images(self, event: wx.Event):
        current_pose = self.get_current_pose()
        if not self.source_image_dirty \
                and self.last_pose is not None \
                and self.last_pose == current_pose \
                and self.last_output_index == self.output_index_choice.GetSelection():
            return
        self.last_pose = current_pose
        self.last_output_index = self.output_index_choice.GetSelection()

        if self.torch_source_image is None or self.poser is None:
            self.draw_nothing_yet_string_to_bitmap(self.source_image_bitmap)
            self.draw_nothing_yet_string_to_bitmap(self.result_image_bitmap)
            self.source_image_dirty = False
            self.Refresh()
            self.Update()
            return

        if self.source_image_dirty:
            dc = wx.MemoryDC()
            dc.SelectObject(self.source_image_bitmap)
            dc.Clear()
            dc.DrawBitmap(self.wx_source_image, 0, 0)
            self.source_image_dirty = False

        pose = torch.tensor(current_pose, device=self.device)
        output_index = self.output_index_choice.GetSelection()
        with torch.no_grad():
            start_cuda_event = torch.cuda.Event(enable_timing=True)
            end_cuda_event = torch.cuda.Event(enable_timing=True)
            start_cuda_event.record()
            start_time = time.time()

            output_image = self.poser.pose(self.torch_source_image, pose, output_index)[0].detach().cpu()

            end_time = time.time()
            end_cuda_event.record()
            torch.cuda.synchronize()
            print("cuda time (ms):", start_cuda_event.elapsed_time(end_cuda_event))
            print("elapsed time (ms):", (end_time - start_time) * 1000.0)

        numpy_image = convert_output_image_from_torch_to_numpy(output_image)
        self.last_output_numpy_image = numpy_image
        wx_image = wx.ImageFromBuffer(
            numpy_image.shape[0],
            numpy_image.shape[1],
            numpy_image[:, :, 0:3].tobytes(),
            numpy_image[:, :, 3].tobytes())
        wx_bitmap = wx_image.ConvertToBitmap()

        dc = wx.MemoryDC()
        dc.SelectObject(self.result_image_bitmap)
        dc.Clear()
        dc.DrawBitmap(wx_bitmap,
                      (MainFrame.IMAGE_SIZE - numpy_image.shape[0]) // 2,
                      (MainFrame.IMAGE_SIZE - numpy_image.shape[1]) // 2,
                      True)
        del dc

        self.Refresh()
        self.Update()

    def on_save_image(self, event: wx.Event):
        if self.last_output_numpy_image is None:
            logging.info("There is no output image to save!!!")
            return

        dir_name = "data/images"
        file_dialog = wx.FileDialog(self, "Choose an image", dir_name, "", "*.png", wx.FD_SAVE)
        if file_dialog.ShowModal() == wx.ID_OK:
            image_file_name = os.path.join(file_dialog.GetDirectory(), file_dialog.GetFilename())
            try:
                if os.path.exists(image_file_name):
                    message_dialog = wx.MessageDialog(self, f"Override {image_file_name}", "Manual Poser",
                                                      wx.YES_NO | wx.ICON_QUESTION)
                    result = message_dialog.ShowModal()
                    if result == wx.ID_YES:
                        self.save_last_numpy_image(image_file_name)
                    message_dialog.Destroy()
                else:
                    self.save_last_numpy_image(image_file_name)
            except:
                message_dialog = wx.MessageDialog(self, f"Could not save {image_file_name}", "Manual Poser", wx.OK)
                message_dialog.ShowModal()
                message_dialog.Destroy()
        file_dialog.Destroy()

    def save_last_numpy_image(self, image_file_name):
        numpy_image = self.last_output_numpy_image
        pil_image = PIL.Image.fromarray(numpy_image, mode='RGBA')
        os.makedirs(os.path.dirname(image_file_name), exist_ok=True)
        pil_image.save(image_file_name)


if __name__ == "__main__":
    device = torch.device('cuda:0')
    app = wx.App()
    main_frame = MainFrame(device)
    main_frame.Show(True)
    main_frame.timer.Start(16)
    app.MainLoop()
