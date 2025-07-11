from typing import List, Optional

import torch
from torch import Tensor
from torch.nn import Module

from tha4.nn.common.poser_encoder_decoder_00 import PoserEncoderDecoder00Args, PoserEncoderDecoder00
from tha4.nn.image_processing_util import apply_color_change, apply_grid_change, apply_rgb_change
from tha4.shion.core.module_factory import ModuleFactory
from tha4.nn.nonlinearity_factory import ReLUFactory
from tha4.nn.normalization import InstanceNorm2dFactory
from tha4.nn.util import BlockArgs


class EyebrowMorphingCombiner00Args(PoserEncoderDecoder00Args):
    def __init__(self,
                 image_size: int = 128,
                 image_channels: int = 4,
                 num_pose_params: int = 12,
                 start_channels: int = 64,
                 bottleneck_image_size=16,
                 num_bottleneck_blocks=6,
                 max_channels: int = 512,
                 block_args: Optional[BlockArgs] = None):
        super().__init__(
            image_size,
            2 * image_channels,
            image_channels,
            num_pose_params,
            start_channels,
            bottleneck_image_size,
            num_bottleneck_blocks,
            max_channels,
            block_args)


class EyebrowMorphingCombiner00(Module):
    def __init__(self, args: EyebrowMorphingCombiner00Args):
        super().__init__()
        self.args = args
        self.body = PoserEncoderDecoder00(args)
        self.morphed_eyebrow_layer_grid_change = self.args.create_grid_change_block()
        self.morphed_eyebrow_layer_alpha = self.args.create_alpha_block()
        self.morphed_eyebrow_layer_color_change = self.args.create_color_change_block()
        self.combine_alpha = self.args.create_alpha_block()

    def forward(self, background_layer: Tensor, eyebrow_layer: Tensor, pose: Tensor, *args) -> List[Tensor]:
        combined_image = torch.cat([background_layer, eyebrow_layer], dim=1)
        feature = self.body(combined_image, pose)[0]

        morphed_eyebrow_layer_grid_change = self.morphed_eyebrow_layer_grid_change(feature)
        morphed_eyebrow_layer_alpha = self.morphed_eyebrow_layer_alpha(feature)
        morphed_eyebrow_layer_color_change = self.morphed_eyebrow_layer_color_change(feature)
        warped_eyebrow_layer = apply_grid_change(morphed_eyebrow_layer_grid_change, eyebrow_layer)
        morphed_eyebrow_layer = apply_color_change(
            morphed_eyebrow_layer_alpha, morphed_eyebrow_layer_color_change, warped_eyebrow_layer)

        combine_alpha = self.combine_alpha(feature)
        eyebrow_image = apply_rgb_change(combine_alpha, morphed_eyebrow_layer, background_layer)
        eyebrow_image_no_combine_alpha = apply_rgb_change(
            (morphed_eyebrow_layer[:, 3:4, :, :] + 1.0) / 2.0, morphed_eyebrow_layer, background_layer)

        return [
            eyebrow_image,  # 0
            combine_alpha,  # 1
            eyebrow_image_no_combine_alpha,  # 2
            morphed_eyebrow_layer,  # 3
            morphed_eyebrow_layer_alpha,  # 4
            morphed_eyebrow_layer_color_change,  # 5
            warped_eyebrow_layer,  # 6
            morphed_eyebrow_layer_grid_change,  # 7
        ]

    EYEBROW_IMAGE_INDEX = 0
    COMBINE_ALPHA_INDEX = 1
    EYEBROW_IMAGE_NO_COMBINE_ALPHA_INDEX = 2
    MORPHED_EYEBROW_LAYER_INDEX = 3
    MORPHED_EYEBROW_LAYER_ALPHA_INDEX = 4
    MORPHED_EYEBROW_LAYER_COLOR_CHANGE_INDEX = 5
    WARPED_EYEBROW_LAYER_INDEX = 6
    MORPHED_EYEBROW_LAYER_GRID_CHANGE_INDEX = 7
    OUTPUT_LENGTH = 8


class EyebrowMorphingCombiner00Factory(ModuleFactory):
    def __init__(self, args: EyebrowMorphingCombiner00Args):
        super().__init__()
        self.args = args

    def create(self) -> Module:
        return EyebrowMorphingCombiner00(self.args)
