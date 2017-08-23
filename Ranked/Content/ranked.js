var ranked = {

  /* Session variables */

  sid: undefined,

  /* Constants */

  session: 'ranked',
  results: { lose: 0, win: 1 },

  /* Cache */

  elements: {
    menu: undefined,
    menuElement: undefined,
    message: undefined,
    loader: undefined,
    container: undefined
  },

  /* Helper methods */

  loader: {
    count: 0,
    start: () => {
      ranked.loader.count++;
      ranked.elements.loader.slideDown();
    },
    end: (force) => {
      ranked.loader.count -= force === true ? ranked.loader.count : 1;
      if (ranked.loader.count === 0) ranked.elements.loader.slideUp('slow');
    }
  },

  json: (url, options) => {
    ranked.loader.start();
    $.ajax(url, $.extend({}, {
      type: 'get',
      dataType: 'json',
      contentType: 'application/json; charset=UTF-8',
      headers: { 'Authorization': ranked.sid },
      data: JSON.stringify(options.value),
      error: (r) => {
        ranked.elements.message.empty();
        if (r.status === 401) return (ranked.sid !== undefined) ? ranked.logout(true) : ranked.elements.message.append($(`<div class="error">Invalid credentials.</div>`));
        else if (r.status === 429) return ranked.elements.message.append($(`<div class="error">Too many requests - please wait...</div>`));
        else {
          ranked.elements.container.empty();
          ranked.elements.message.append($(`<div class="error">Error. Please reload and try again.</div>`));
        }
      },
      complete: ranked.loader.end
    }, options));
  },

  /* Initialisation */

  init: () => {
    ranked.sid = ranked.sid || Cookies.get(ranked.session);

    ranked.elements.menu = (ranked.elements.menu || $('#menu')).empty();
    ranked.elements.message = (ranked.elements.message || $('#message')).empty();
    ranked.elements.loader = ranked.elements.loader || $('#loader');
    ranked.elements.container = (ranked.elements.container || $('#container')).empty();

    if (ranked.sid === undefined) return ranked.showLogin();

    swipe.config.single = false;
    swipe.config.snapFrom = 50;
    swipe.config.snapTo = 100;
    
    ranked.menu();
    ranked.load();
  },

  menu: () => {
    $(`<div><a href="#" onclick="ranked.elements.menuElement.toggle('slow'); return false;"><img src='Content/img/menu.png' style="width: 32px; height: 32px; margin-bottom: -10px; margin-left: -10px;" /></a></div>`).appendTo(ranked.elements.menu);
    ranked.elements.menuElement = $(`<div></div>`).hide().appendTo(ranked.elements.menu);
    ranked.elements.menuElement.append($(`<form onsubmit="ranked.password(); return false;"><label for="old">Password</label><input type="submit" value="Ok" /><div><input id="old" type="password" placeholder="Old" required /><input id="new" type="password" placeholder="New" required /></div></form>`));
    ranked.elements.menuElement.append($(`<form onsubmit="ranked.logout(); return false;" class="stretch"><div><input type="submit" value="Log out" /></div></form>`));
  },

  load: () => {
    ranked.json('/users', { success: (r) => { for (var i = 0; i < r.length; i++) ranked.render(r[i]); } });
  },

  /* Login / logout functionality */

  showLogin: () => {
    ranked.loader.end(true);
    ranked.elements.container.append($('<form class="login" onsubmit="ranked.login(); return false;"><input id="id" type="email" placeholder="Email Address" required /><input id="password" type="password" placeholder="Password" required /><input type="submit" value="Log in" /><div class="register">New user? Register <a href="#" onclick="ranked.showRegister(); return false;">here</a>.</div></form>'));
  },

  login: () => {
    ranked.json('/sessions', {
      type: 'post', value: { id: $('#id').val(), password: $('#password').val() }, success: (r) => { Cookies.set(ranked.session, r.id, { expires: 90 }); ranked.init(); }
    });
  },

  logout: (local) => {
    if (local === undefined) ranked.json('/sessions', { type: 'delete', data: null, error: null, complete: ranked.logout });
    else {
      Cookies.remove(ranked.session);
      ranked.sid = undefined;
      ranked.init();
    }
  },

  /* Register functionality */

  showRegister: () => {
    ranked.elements.container.empty();
    ranked.elements.container.append($('<form class="login" onsubmit="ranked.register(); return false;"><input id="id" type="email" placeholder="Email Address" required /><input id="password" type="password" placeholder="Password" required /><input type="submit" value="Register" /></form>'));
  },

  register: () => {
    ranked.json('/users', { type: 'post', value: { id: $('#id').val(), password: $('#password').val() }, success: ranked.login });
  },

  /* Menu handlers */

  password: () => {
    ranked.json('/users', { type: 'put', value: { password: $('#new').val() }, success: ranked.init });
  },

  /* Render users */

  render: (value) => {
    var cls = "user";
    if (value.me) cls += " me"; // it's you, just for reference
    if (value.pendingLoss || value.pendingWin) cls += " pending"; // you're waiting on the other guy to confirm
    if (value.confirmLoss || value.confirmWin) cls += " confirm"; // you need to confirm
    if (value.provisional) cls += " provisional"; // provisional player

    var lose = value.pendingLoss ? 'Pending' : (value.confirmLoss ? 'Confirm' : 'Lose');
    var win = value.pendingWin ? 'Pending' : (value.confirmWin ? 'Confirm' : 'Win');

    var div = $(`<div class="${cls}">${value.id.slice(0, -13)}</div>`);
    var wrapper = $(`<div></div>`).hide().appendTo(ranked.elements.container);

    if (!value.me) {
      wrapper
        .append($(`<a class="lose" href="#" onclick="ranked.match('${value.id}', ranked.results.lose); return false;">${lose}</a>`))
        .append($(`<a class="win" href="#" onclick="ranked.match('${value.id}', ranked.results.win); return false;">${win}</a>`));
    }

    wrapper.append(div)
      .append($(`<span class="win">${value.wins}W.${value.losses}L</span>`))
      .append($(`<span class="win">${value.rating}</span>`));

    swipe.initElements(div);
    wrapper.show();
  },

  /* Name (rating) handler */

  match: (name, result) => {
    ranked.json('/match', {
      type: 'post',
      value: {
        winner: (result == ranked.results.lose) ? name : undefined,
        loser: (result == ranked.results.win) ? name : undefined
      },
      success: () => {
        ranked.elements.container.empty();
        ranked.load();
      }
    });
  }
};

$('document').ready(ranked.init);