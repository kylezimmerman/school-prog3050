// Stripe setup script
(function () {
    Stripe.setPublishableKey('pk_test_p7evtCqVwwsVMpTl4yqb1Ndh');

    function stripeResponseHandler(status, response) {
        var $form = $('#addCardForm');

        if (response.error) {
            console.log(response);
            console.log(response.error);

            // Show the errors on the form
            $form.find('.payment-errors').
                text(response.error.message).
                removeClass('field-validation-valid').
                addClass('field-validation-error');

            $form.find('button').prop('disabled', false);
        }
        else {
            // Remove all name attributes before attempting a submit
            $form.find(':input[data-stripe]').each(function () {
                $(this).removeAttr('name');
            });

            // response contains id and card, which contains additional card details
            var token = response.id;
            // Insert the token into the form so it gets submitted to the server
            $form.append($('<input type="hidden" name="stripeCardToken" />').val(token));

            // and submit
            $form.get(0).submit();
        }
    };

    $(function ($) {
        var $form = $('#addCardForm');

        // Remove the display: none style from the form.
        $form.css('display', 'block');

        // Add back name attributes
        $form.find(':input[data-stripe]').each(function () {
            $(this).attr('name', this.id.replace('_', '.'));
        });

        // Remove the default validation
        $form.removeData('validator').removeData('unobtrusiveValidation');

        // Reparse the form for validation
        $.validator.unobtrusive.parse('#addCardForm');

        $form.submit(function (event) {
            var $form = $(this);

            if ($form.valid()) {
                $form.find('.payment-errors').
                    text('').
                    removeClass('field-validation-error').
                    addClass('field-validation-valid');

                // Disable the submit button to prevent repeated clicks
                $form.find('button').prop('disabled', true);

                Stripe.card.createToken($form, stripeResponseHandler);

                // Prevent the form from submitting with the default action
                return false;
            }

            return false;
        });
    });
})();