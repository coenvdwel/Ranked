/*!
 * Swipe v1.0
 * https://github.com/coenvdwel/unamit
 *
 * Copyright 2017 Coen van der Wel
 * Released under the MIT license
 *
 * Todo / Wishlist:
 * * Overextension choses left-/rightmost option
 * * Preload data on initialize
 */

var swipe =
  {
    config:
    {
      single: true, // only allow a single extended element
      snapFrom: 30, // snaps back before this point, or to `snapTo` beyond it
      snapTo: 60 // fully extended width
    },
    session:
    {
      swiping: false
    },
    init: () => {
      swipe.initElements($('.swipe'));
      $('body').on('mouseup touchend', swipe.end)
        .on('mousemove touchmove', swipe.move);
    },
    initElements: (e) => {
      e.on('mousedown touchstart', swipe.start)
        .css({ 'float': 'left', 'display': 'block', 'width': '100%', 'overflow': 'hidden', 'text-overflow': 'ellipsis', 'white-space': 'nowrap' })
        .siblings().css({ 'float': 'left', 'display': 'block', 'overflow': 'hidden', 'width': 0 });
    },
    start: (e) => {
      var state = 0, target = $(e.target);

      if (swipe.session.swiping) return;
      if (swipe.config.single && swipe.session.target !== undefined && swipe.session.target[0] !== target[0]) swipe.set(0);

      swipe.session =
        {
          swiping: true,
          target: target,
          width: target.outerWidth(),
          from: e.clientX || e.originalEvent.touches[0].clientX,
          elements: { left: target.prevAll().outerHeight(target.outerHeight()), right: target.nextAll().outerHeight(target.outerHeight()) }
        };

      if (swipe.session.elements.left.outerWidth() > 0) state = -1;
      else if (swipe.session.elements.right.outerWidth() > 0) state = 1;
      else return;

      var elements = (state < 0) ? swipe.session.elements.left : swipe.session.elements.right;
      var d = elements.outerWidth() * elements.length;
      swipe.session.width += d, swipe.session.from += state * d;
    },
    end: (e) => {
      if (!swipe.session.swiping) return;
      swipe.session.swiping = false;

      var clicked = swipe.session.by === undefined;
      if (clicked) {
        if (swipe.session.elements.left.outerWidth() > 0 || swipe.session.elements.right.outerWidth() > 0) return swipe.set(0);
        swipe.session.by = Math.sign(swipe.session.width / 2 - (swipe.session.from - swipe.session.target.offset().left));
      }

      var elements = swipe.session.by > 0 ? swipe.session.elements.left : swipe.session.elements.right;
      swipe.set(elements.outerWidth() < swipe.config.snapFrom && !clicked ? 0 : Math.sign(swipe.session.by) * elements.length * swipe.config.snapTo);
    },
    move: (e) => {
      if (!swipe.session.swiping) return;

      swipe.session.to = e.clientX || e.originalEvent.touches[0].clientX;
      swipe.set(swipe.session.to - swipe.session.from);
    },
    set: (e) => {
      var snapTo = swipe.config.snapTo * (e < 0 ? swipe.session.elements.right.length : swipe.session.elements.left.length);

      swipe.session.by = Math.abs(e) < snapTo ? e : Math.sign(e) * (snapTo + (Math.abs(e) - snapTo) / 4);
      swipe.session.target.outerWidth(swipe.session.width - Math.abs(swipe.session.by));
      swipe.session.elements.left.outerWidth(Math.abs(Math.max(0, swipe.session.by)) / swipe.session.elements.left.length);
      swipe.session.elements.right.outerWidth(Math.abs(Math.min(0, swipe.session.by)) / swipe.session.elements.right.length);
    }
  };

$('document').ready(swipe.init);