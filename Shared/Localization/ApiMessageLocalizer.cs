using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SaborExpress.Shared.Localization;

public sealed class ApiMessageLocalizer : IApiMessageLocalizer
{
    private sealed record LocalizedMessage(string English, string Portuguese, string French);

    // Match the MVC response defaults so wrapping an API payload in JsonNode
    // does not change property casing or turn string enums into numbers.
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly IReadOnlyDictionary<string, LocalizedMessage> Messages =
        new Dictionary<string, LocalizedMessage>(StringComparer.OrdinalIgnoreCase)
        {
            ["Credenciales incorrectas."] = new("Incorrect credentials.", "Credenciais incorretas.", "Identifiants incorrects."),
            ["Debes ingresar tu correo o documento."] = new("Enter your email or ID number.", "Informe seu e-mail ou documento.", "Saisissez votre adresse e-mail ou votre pièce d'identité."),
            ["El correo es obligatorio"] = new("Email is required.", "O e-mail é obrigatório.", "L'adresse e-mail est obligatoire."),
            ["El correo electrónico no tiene un formato válido."] = new("The email address is not valid.", "O formato do e-mail é inválido.", "Le format de l'adresse e-mail est invalide."),
            ["El correo ya está registrado."] = new("This email is already registered.", "Este e-mail já está cadastrado.", "Cette adresse e-mail est déjà enregistrée."),
            ["El código debe tener 6 dígitos."] = new("The code must contain 6 digits.", "O código deve ter 6 dígitos.", "Le code doit comporter 6 chiffres."),
            ["Código invalido o expirado."] = new("The code is invalid or has expired.", "O código é inválido ou expirou.", "Le code est invalide ou a expiré."),
            ["Codigo invalido o expirado."] = new("The code is invalid or has expired.", "O código é inválido ou expirou.", "Le code est invalide ou a expiré."),
            ["Enlace invalido o expirado."] = new("The link is invalid or has expired.", "O link é inválido ou expirou.", "Le lien est invalide ou a expiré."),
            ["El enlace de activacion es invalido"] = new("The activation link is invalid.", "O link de ativação é inválido.", "Le lien d'activation est invalide."),
            ["El codigo de recuperacion es invalido o ha expirado."] = new("The recovery code is invalid or has expired.", "O código de recuperação é inválido ou expirou.", "Le code de récupération est invalide ou a expiré."),
            ["La contraseña debe tener al menos 8 caracteres."] = new("The password must contain at least 8 characters.", "A senha deve ter pelo menos 8 caracteres.", "Le mot de passe doit comporter au moins 8 caractères."),
            ["La cuenta esta deshabilitada."] = new("This account is disabled.", "Esta conta está desativada.", "Ce compte est désactivé."),
            ["Se ha enviado un codigo de recuperacion a tu correo."] = new("A recovery code has been sent to your email.", "Um código de recuperação foi enviado para seu e-mail.", "Un code de récupération a été envoyé à votre adresse e-mail."),
            ["Contraseña actualizada correctamente."] = new("Password updated successfully.", "Senha atualizada com sucesso.", "Mot de passe mis à jour."),
            ["Sesion cerrada correctamente."] = new("Signed out successfully.", "Sessão encerrada com sucesso.", "Déconnexion réussie."),
            ["Correo confirmado correctamente. Ya puedes iniciar sesion."] = new("Email confirmed. You can now sign in.", "E-mail confirmado. Agora você pode entrar.", "Adresse e-mail confirmée. Vous pouvez maintenant vous connecter."),
            ["Si el correo existe y no ha sido confirmado, se enviara un nuevo codigo."] = new("If the email exists and is not confirmed, a new code will be sent.", "Se o e-mail existir e ainda não tiver sido confirmado, um novo código será enviado.", "Si cette adresse existe et n'a pas été confirmée, un nouveau code sera envoyé."),
            ["Si la cuenta existe y no ha sido activada, se enviara un nuevo codigo."] = new("If the account exists and is not active, a new code will be sent.", "Se a conta existir e ainda não estiver ativa, um novo código será enviado.", "Si ce compte existe et n'est pas activé, un nouveau code sera envoyé."),
            ["Cuenta activada correctamente. Ya puedes iniciar sesion con tu nueva contrasena."] = new("Account activated. You can now sign in with your new password.", "Conta ativada. Agora você pode entrar com sua nova senha.", "Compte activé. Vous pouvez maintenant vous connecter avec votre nouveau mot de passe."),
            ["No se pudo identificar al empleado autenticado."] = new("Could not identify the authenticated employee.", "Não foi possível identificar o funcionário autenticado.", "Impossible d'identifier l'employé authentifié."),
            ["No se pudo identificar al cliente autenticado."] = new("Could not identify the authenticated customer.", "Não foi possível identificar o cliente autenticado.", "Impossible d'identifier le client authentifié."),
            ["No se pudo identificar al usuario autenticado."] = new("Could not identify the authenticated user.", "Não foi possível identificar o usuário autenticado.", "Impossible d'identifier l'utilisateur authentifié."),
            ["Token inválido: no contiene el identificador del usuario."] = new("The token is invalid: it does not contain a user ID.", "O token é inválido: não contém o identificador do usuário.", "Le jeton est invalide : il ne contient pas l'identifiant de l'utilisateur."),
            ["Este usuario no tiene un EmployeeId en su token."] = new("This user has no EmployeeId in their token.", "Este usuário não tem um EmployeeId no token.", "Cet utilisateur n'a pas d'EmployeeId dans son jeton."),
            ["Usuario actual no encontrado."] = new("Current user not found.", "Usuário atual não encontrado.", "Utilisateur actuel introuvable."),
            ["El usuario no existe."] = new("The user does not exist.", "O usuário não existe.", "L'utilisateur n'existe pas."),
            ["El cliente no existe."] = new("The customer does not exist.", "O cliente não existe.", "Le client n'existe pas."),
            ["El cliente no existe"] = new("The customer does not exist.", "O cliente não existe.", "Le client n'existe pas."),
            ["El empleado no existe."] = new("The employee does not exist.", "O funcionário não existe.", "L'employé n'existe pas."),
            ["El empleado no existe"] = new("The employee does not exist.", "O funcionário não existe.", "L'employé n'existe pas."),
            ["El empleado indicado no es un repartidor activo."] = new("The selected employee is not an active delivery driver.", "O funcionário selecionado não é um entregador ativo.", "L'employé sélectionné n'est pas un livreur actif."),
            ["El empleado no tiene una cuenta de usuario asociada."] = new("The employee has no associated user account.", "O funcionário não tem uma conta de usuário associada.", "Aucun compte utilisateur n'est associé à cet employé."),
            ["La sucursal no existe"] = new("The branch does not exist.", "A unidade não existe.", "L'établissement n'existe pas."),
            ["La sucursal no existe."] = new("The branch does not exist.", "A unidade não existe.", "L'établissement n'existe pas."),
            ["La sucursal seleccionada no existe"] = new("The selected branch does not exist.", "A unidade selecionada não existe.", "L'établissement sélectionné n'existe pas."),
            ["El pedido no existe"] = new("The order does not exist.", "O pedido não existe.", "La commande n'existe pas."),
            ["El pedido no existe."] = new("The order does not exist.", "O pedido não existe.", "La commande n'existe pas."),
            ["El pedido no tiene productos."] = new("The order has no products.", "O pedido não tem produtos.", "La commande ne contient aucun produit."),
            ["Debe incluir al menos un producto en el pedido."] = new("Add at least one product to the order.", "Adicione pelo menos um produto ao pedido.", "Ajoutez au moins un produit à la commande."),
            ["El producto no existe"] = new("The product does not exist.", "O produto não existe.", "Le produit n'existe pas."),
            ["El producto no existe."] = new("The product does not exist.", "O produto não existe.", "Le produit n'existe pas."),
            ["El producto esta agotado y no se puede agregar al pedido"] = new("This product is out of stock and cannot be added to the order.", "Este produto está esgotado e não pode ser adicionado ao pedido.", "Ce produit est en rupture de stock et ne peut pas être ajouté à la commande."),
            ["El precio debe ser mayor a cero"] = new("The price must be greater than zero.", "O preço deve ser maior que zero.", "Le prix doit être supérieur à zéro."),
            ["El precio debe ser mayor a cero."] = new("The price must be greater than zero.", "O preço deve ser maior que zero.", "Le prix doit être supérieur à zéro."),
            ["El nombre del producto es obligatorio"] = new("The product name is required.", "O nome do produto é obrigatório.", "Le nom du produit est obligatoire."),
            ["El nombre de la categoría es obligatorio"] = new("The category name is required.", "O nome da categoria é obrigatório.", "Le nom de la catégorie est obligatoire."),
            ["La categoría seleccionada no existe"] = new("The selected category does not exist.", "A categoria selecionada não existe.", "La catégorie sélectionnée n'existe pas."),
            ["La cantidad debe ser mayor a cero"] = new("The quantity must be greater than zero.", "A quantidade deve ser maior que zero.", "La quantité doit être supérieure à zéro."),
            ["Debe indicar un producto valido"] = new("Select a valid product.", "Informe um produto válido.", "Sélectionnez un produit valide."),
            ["Debe indicar un producto válido."] = new("Select a valid product.", "Informe um produto válido.", "Sélectionnez un produit valide."),
            ["Debe indicar un pedido valido"] = new("Select a valid order.", "Informe um pedido válido.", "Sélectionnez une commande valide."),
            ["Debe indicar un pedido válido"] = new("Select a valid order.", "Informe um pedido válido.", "Sélectionnez une commande valide."),
            ["El número de mesa debe ser mayor a cero"] = new("The table number must be greater than zero.", "O número da mesa deve ser maior que zero.", "Le numéro de table doit être supérieur à zéro."),
            ["Un pedido en el local (DineIn) debe indicar una mesa"] = new("A dine-in order must include a table.", "Um pedido para consumo no local deve indicar uma mesa.", "Une commande sur place doit indiquer une table."),
            ["La mesa no existe en esta sucursal"] = new("The table does not exist at this branch.", "A mesa não existe nesta unidade.", "La table n'existe pas dans cet établissement."),
            ["No se puede editar un pedido entregado o cancelado"] = new("A delivered or canceled order cannot be edited.", "Um pedido entregue ou cancelado não pode ser editado.", "Une commande livrée ou annulée ne peut pas être modifiée."),
            ["Debe indicar el motivo de la cancelación"] = new("Enter a reason for the cancellation.", "Informe o motivo do cancelamento.", "Indiquez le motif de l'annulation."),
            ["Debe indicar el motivo del reembolso"] = new("Enter a reason for the refund.", "Informe o motivo do reembolso.", "Indiquez le motif du remboursement."),
            ["El porcentaje de Impoconsumo debe estar entre 0 y 100."] = new("The consumption tax rate must be between 0 and 100.", "A alíquota do imposto sobre consumo deve estar entre 0 e 100.", "Le taux de taxe à la consommation doit être compris entre 0 et 100."),
            ["El radio de entrega debe estar entre 0.01 y 100 km."] = new("The delivery radius must be between 0.01 and 100 km.", "O raio de entrega deve estar entre 0,01 e 100 km.", "Le rayon de livraison doit être compris entre 0,01 et 100 km."),
            ["La hora de fin debe ser mayor a la hora de inicio."] = new("The end time must be later than the start time.", "O horário de término deve ser posterior ao horário de início.", "L'heure de fin doit être postérieure à l'heure de début."),
            ["La hora de inicio debe ser menor a la hora de fin."] = new("The start time must be earlier than the end time.", "O horário de início deve ser anterior ao horário de término.", "L'heure de début doit être antérieure à l'heure de fin."),
            ["Debes indicar la fecha del turno."] = new("Enter the shift date.", "Informe a data do turno.", "Indiquez la date du service."),
            ["Debes indicar un empleado valido."] = new("Select a valid employee.", "Informe um funcionário válido.", "Sélectionnez un employé valide."),
            ["Debes indicar una sucursal valida."] = new("Select a valid branch.", "Informe uma unidade válida.", "Sélectionnez un établissement valide."),
            ["Debes indicar un rol valido para el turno."] = new("Select a valid role for the shift.", "Informe um cargo válido para o turno.", "Sélectionnez un rôle valide pour le service."),
            ["El empleado ya tiene un turno asignado que se cruza en ese horario."] = new("The employee already has a shift that overlaps this time.", "O funcionário já tem um turno que coincide com esse horário.", "L'employé a déjà un service qui chevauche cet horaire."),
            ["Solo el Gerente puede trasladar un empleado de sede."] = new("Only a manager can transfer an employee to another branch.", "Somente um gerente pode transferir um funcionário para outra unidade.", "Seul un responsable peut transférer un employé vers un autre établissement."),
            ["El rol especificado no existe."] = new("The selected role does not exist.", "O cargo selecionado não existe.", "Le rôle sélectionné n'existe pas."),
            ["El rol especificado no existe"] = new("The selected role does not exist.", "O cargo selecionado não existe.", "Le rôle sélectionné n'existe pas."),
            ["El rol no existe."] = new("The role does not exist.", "O cargo não existe.", "Le rôle n'existe pas."),
            ["El rol CLIENTE no está configurado en el sistema."] = new("The CUSTOMER role is not configured in the system.", "O cargo CLIENTE não está configurado no sistema.", "Le rôle CLIENT n'est pas configuré dans le système."),
            ["No tienes permiso para asignar roles."] = new("You do not have permission to assign roles.", "Você não tem permissão para atribuir cargos.", "Vous n'avez pas l'autorisation d'attribuer des rôles."),
            ["No tienes permiso para gestionar turnos."] = new("You do not have permission to manage shifts.", "Você não tem permissão para gerenciar turnos.", "Vous n'avez pas l'autorisation de gérer les services."),
            ["No tienes permiso para cambiar el estado de las mesas ahora mismo."] = new("You do not have permission to change table status right now.", "Você não tem permissão para alterar o estado das mesas agora.", "Vous n'avez pas l'autorisation de modifier l'état des tables pour le moment."),
            ["No tienes permiso para activar/desactivar productos ahora mismo."] = new("You do not have permission to activate or deactivate products right now.", "Você não tem permissão para ativar ou desativar produtos agora.", "Vous n'avez pas l'autorisation d'activer ou de désactiver les produits pour le moment."),
            ["No tienes permiso para cambiar el estado de los pedidos."] = new("You do not have permission to change order status.", "Você não tem permissão para alterar o status dos pedidos.", "Vous n'avez pas l'autorisation de modifier l'état des commandes."),
            ["El turno no existe."] = new("The shift does not exist.", "O turno não existe.", "Le service n'existe pas."),
            ["La configuración no existe"] = new("The configuration does not exist.", "A configuração não existe.", "La configuration n'existe pas."),
            ["La preferencia no existe"] = new("The preference does not exist.", "A preferência não existe.", "La préférence n'existe pas."),
            ["Esta preferencia no pertenece al usuario"] = new("This preference does not belong to this user.", "Esta preferência não pertence a este usuário.", "Cette préférence n'appartient pas à cet utilisateur."),
            ["El idioma es obligatorio"] = new("Language is required.", "O idioma é obrigatório.", "La langue est obligatoire."),
            ["No se pudo guardar la configuración. Intenta de nuevo."] = new("The settings could not be saved. Please try again.", "Não foi possível salvar as configurações. Tente novamente.", "Impossible d'enregistrer les paramètres. Réessayez."),
            ["Debes especificar UserId o BranchId como destinatario."] = new("Specify a user or branch as the recipient.", "Informe um usuário ou uma unidade como destinatário.", "Indiquez un utilisateur ou un établissement comme destinataire."),
            ["No hay empleados activos en esa sucursal."] = new("There are no active employees at this branch.", "Não há funcionários ativos nesta unidade.", "Il n'y a aucun employé actif dans cet établissement."),
            ["Notificación no encontrada."] = new("Notification not found.", "Notificação não encontrada.", "Notification introuvable."),
            ["No puedes marcar como leída una notificación que no es tuya."] = new("You cannot mark another user's notification as read.", "Você não pode marcar como lida uma notificação de outro usuário.", "Vous ne pouvez pas marquer comme lue une notification qui ne vous appartient pas."),
            ["No puedes eliminar una notificación que no es tuya."] = new("You cannot delete another user's notification.", "Você não pode excluir uma notificação de outro usuário.", "Vous ne pouvez pas supprimer une notification qui ne vous appartient pas."),
            ["El token push de Expo no es válido."] = new("The Expo push token is invalid.", "O token push do Expo é inválido.", "Le jeton push Expo est invalide."),
            ["No tienes permiso para ver esta sede."] = new("You do not have permission to view this branch.", "Você não tem permissão para ver esta unidade.", "Vous n'avez pas l'autorisation de consulter cet établissement."),
            ["Solo puedes ver las entregas de tu propia sede."] = new("You can only view deliveries for your own branch.", "Você só pode ver as entregas da sua unidade.", "Vous ne pouvez consulter que les livraisons de votre établissement."),
            ["No puedes ver las entregas de otro repartidor"] = new("You cannot view another delivery driver's deliveries.", "Você não pode ver as entregas de outro entregador.", "Vous ne pouvez pas consulter les livraisons d'un autre livreur."),
            ["Solo puedes ver los repartidores de tu propia sede"] = new("You can only view delivery drivers at your own branch.", "Você só pode ver os entregadores da sua unidade.", "Vous ne pouvez consulter que les livreurs de votre établissement."),
            ["Solo puedes ver la configuración de tu propia sede"] = new("You can only view or change settings for your own branch.", "Você só pode ver ou alterar as configurações da sua unidade.", "Vous ne pouvez consulter ou modifier que les paramètres de votre établissement."),
            ["Solo puedes crear configuración para tu propia sede"] = new("You can only create settings for your own branch.", "Você só pode criar configurações para sua unidade.", "Vous ne pouvez créer des paramètres que pour votre établissement."),
            ["Solo puedes editar la configuración de tu propia sede"] = new("You can only edit settings for your own branch.", "Você só pode editar as configurações da sua unidade.", "Vous ne pouvez modifier les paramètres que de votre établissement."),
            ["Ese repartidor no pertenece a tu sede"] = new("That delivery driver does not belong to your branch.", "Esse entregador não pertence à sua unidade.", "Ce livreur n'appartient pas à votre établissement."),
            ["Solo puedes ver los repartos de tu propia sede"] = new("You can only view deliveries for your own branch.", "Você só pode ver as entregas da sua unidade.", "Vous ne pouvez consulter que les livraisons de votre établissement."),
            ["Gerente y Administrador no pueden cambiar el estado de los pedidos."] = new("Managers and administrators cannot change order status.", "Gerentes e administradores não podem alterar o status dos pedidos.", "Les responsables et administrateurs ne peuvent pas modifier l'état des commandes."),
            ["Gerente y Administrador no pueden cancelar pedidos."] = new("Managers and administrators cannot cancel orders.", "Gerentes e administradores não podem cancelar pedidos.", "Les responsables et administrateurs ne peuvent pas annuler les commandes."),
            ["El cliente está pagando en línea con Wompi. El pedido se confirmará solo cuando el pago sea aprobado."] = new("The customer is paying online with Wompi. The order will be confirmed only after the payment is approved.", "O cliente está pagando on-line com Wompi. O pedido será confirmado somente após a aprovação do pagamento.", "Le client paie en ligne avec Wompi. La commande sera confirmée uniquement après l'approbation du paiement."),
            ["El cliente no tiene una dirección predeterminada."] = new("The customer has no default address.", "O cliente não tem um endereço padrão.", "Le client n'a pas d'adresse par défaut."),
            ["Este empleado no tiene CV cargado"] = new("This employee has no résumé on file.", "Este funcionário não tem currículo cadastrado.", "Aucun CV n'est enregistré pour cet employé."),
            ["El pago no existe"] = new("The payment does not exist.", "O pagamento não existe.", "Le paiement n'existe pas."),
            ["El pago no existe."] = new("The payment does not exist.", "O pagamento não existe.", "Le paiement n'existe pas."),
            ["El monto debe ser mayor a cero"] = new("The amount must be greater than zero.", "O valor deve ser maior que zero.", "Le montant doit être supérieur à zéro."),
            ["El motivo no puede superar los 500 caracteres"] = new("The reason cannot exceed 500 characters.", "O motivo não pode exceder 500 caracteres.", "Le motif ne peut pas dépasser 500 caractères."),
            ["El correo no tiene un formato válido."] = new("The email address is not valid.", "O formato do e-mail é inválido.", "Le format de l'adresse e-mail est invalide."),
            ["No se pudo guardar la sede"] = new("The branch could not be saved.", "Não foi possível salvar a unidade.", "Impossible d'enregistrer l'établissement."),
            ["No se pudo guardar el producto"] = new("The product could not be saved.", "Não foi possível salvar o produto.", "Impossible d'enregistrer le produit."),
            ["No se pudo guardar la mesa"] = new("The table could not be saved.", "Não foi possível salvar a mesa.", "Impossible d'enregistrer la table."),
            ["No se pudo guardar el turno"] = new("The shift could not be saved.", "Não foi possível salvar o turno.", "Impossible d'enregistrer le service."),
            ["No se pudo procesar el reembolso"] = new("The refund could not be processed.", "Não foi possível processar o reembolso.", "Impossible de traiter le remboursement."),
            ["No se pudo cambiar el estado"] = new("The status could not be changed.", "Não foi possível alterar o status.", "Impossible de modifier le statut."),
            ["No se pudo completar la solicitud."] = new("The request could not be completed.", "Não foi possível concluir a solicitação.", "La demande n'a pas pu être traitée."),
            ["No se pudo completar la solicitud"] = new("The request could not be completed.", "Não foi possível concluir a solicitação.", "La demande n'a pas pu être traitée."),
            ["Categoría no encontrada"] = new("Category not found.", "Categoria não encontrada.", "Catégorie introuvable."),
            ["Debe indicar el motivo de la anulacion"] = new("Enter a reason for voiding this item.", "Informe o motivo do cancelamento deste item.", "Indiquez le motif de l'annulation de cet article."),
            ["Debe indicar un pago válido"] = new("Select a valid payment.", "Selecione um pagamento válido.", "Sélectionnez un paiement valide."),
            ["Debe indicar una fecha válida."] = new("Enter a valid date.", "Informe uma data válida.", "Saisissez une date valide."),
            ["Debe indicar una sucursal válida"] = new("Select a valid branch.", "Selecione uma unidade válida.", "Sélectionnez un établissement valide."),
            ["Debe indicar una sucursal válida."] = new("Select a valid branch.", "Selecione uma unidade válida.", "Sélectionnez un établissement valide."),
            ["Demasiados intentos. Solicita un nuevo codigo."] = new("Too many attempts. Request a new code.", "Muitas tentativas. Solicite um novo código.", "Trop de tentatives. Demandez un nouveau code."),
            ["El apellido no puede contener números."] = new("The last name cannot contain numbers.", "O sobrenome não pode conter números.", "Le nom de famille ne peut pas contenir de chiffres."),
            ["El apellido no puede estar vacío ni contener números."] = new("The last name cannot be empty or contain numbers.", "O sobrenome não pode ficar vazio nem conter números.", "Le nom de famille ne peut être vide ni contenir de chiffres."),
            ["El campo ingresado es demasiado largo."] = new("The entered value is too long.", "O valor informado é longo demais.", "La valeur saisie est trop longue."),
            ["El codigo debe tener 6 digitos."] = new("The code must contain 6 digits.", "O código deve conter 6 dígitos.", "Le code doit comporter 6 chiffres."),
            ["El CV debe ser un archivo PDF"] = new("The résumé must be a PDF file.", "O currículo deve ser um arquivo PDF.", "Le CV doit être un fichier PDF."),
            ["El documento es obligatorio"] = new("The ID number is required.", "O documento é obrigatório.", "Le numéro de pièce d'identité est obligatoire."),
            ["El domicilio no existe"] = new("The delivery does not exist.", "A entrega não existe.", "La livraison n'existe pas."),
            ["El domicilio no existe."] = new("The delivery does not exist.", "A entrega não existe.", "La livraison n'existe pas."),
            ["El empleado que intenta crear el pedido no existe."] = new("The employee creating this order does not exist.", "O funcionário que está criando o pedido não existe.", "L'employé qui crée cette commande n'existe pas."),
            ["El lote indicado no existe para este pedido."] = new("The selected batch does not exist for this order.", "O lote selecionado não existe para este pedido.", "Le lot sélectionné n'existe pas pour cette commande."),
            ["El nombre completo es obligatorio"] = new("The full name is required.", "O nome completo é obrigatório.", "Le nom complet est obligatoire."),
            ["El nombre de la sede es obligatorio."] = new("The branch name is required.", "O nome da unidade é obrigatório.", "Le nom de l'établissement est obligatoire."),
            ["El nombre del permiso es obligatorio."] = new("The permission name is required.", "O nome da permissão é obrigatório.", "Le nom de l'autorisation est obligatoire."),
            ["El nombre del rol es obligatorio."] = new("The role name is required.", "O nome do cargo é obrigatório.", "Le nom du rôle est obligatoire."),
            ["El nombre no puede estar vacío ni contener números."] = new("The name cannot be empty or contain numbers.", "O nome não pode ficar vazio nem conter números.", "Le nom ne peut être vide ni contenir de chiffres."),
            ["El PDF no debe superar 5MB"] = new("The PDF file cannot exceed 5 MB.", "O arquivo PDF não pode exceder 5 MB.", "Le fichier PDF ne peut pas dépasser 5 Mo."),
            ["El pedido no tiene un total válido."] = new("The order total is invalid.", "O total do pedido é inválido.", "Le total de la commande est invalide."),
            ["El permiso no existe"] = new("The permission does not exist.", "A permissão não existe.", "L'autorisation n'existe pas."),
            ["El permiso no existe."] = new("The permission does not exist.", "A permissão não existe.", "L'autorisation n'existe pas."),
            ["El repartidor debe pertenecer a la misma sede del pedido."] = new("The delivery driver must belong to the same branch as the order.", "O entregador deve pertencer à mesma unidade do pedido.", "Le livreur doit appartenir au même établissement que la commande."),
            ["El repartidor indicado no existe o no tiene sede asignada."] = new("The selected delivery driver does not exist or has no branch assigned.", "O entregador selecionado não existe ou não tem uma unidade atribuída.", "Le livreur sélectionné n'existe pas ou n'a aucun établissement attribué."),
            ["El rol no existe"] = new("The role does not exist.", "O cargo não existe.", "Le rôle n'existe pas."),
            ["El teléfono de la sede es obligatorio."] = new("The branch phone number is required.", "O telefone da unidade é obrigatório.", "Le téléphone de l'établissement est obligatoire."),
            ["El teléfono debe tener 7 dígitos (fijo) o 10 dígitos (celular), sin espacios ni guiones."] = new("The phone number must have 7 digits for a landline or 10 for a mobile, with no spaces or hyphens.", "O telefone deve ter 7 dígitos para linha fixa ou 10 para celular, sem espaços ou hífens.", "Le numéro doit comporter 7 chiffres pour une ligne fixe ou 10 pour un mobile, sans espaces ni tirets."),
            ["El tiempo de preparación no puede ser negativo"] = new("Preparation time cannot be negative.", "O tempo de preparo não pode ser negativo.", "Le temps de préparation ne peut pas être négatif."),
            ["Falta Wompi:EventsSecret en la configuración"] = new("Wompi:EventsSecret is missing from the configuration.", "Wompi:EventsSecret não foi configurado.", "Wompi:EventsSecret est absent de la configuration."),
            ["Falta Wompi:IntegritySecret en la configuración"] = new("Wompi:IntegritySecret is missing from the configuration.", "Wompi:IntegritySecret não foi configurado.", "Wompi:IntegritySecret est absent de la configuration."),
            ["Falta Wompi:PrivateKey en la configuración"] = new("Wompi:PrivateKey is missing from the configuration.", "Wompi:PrivateKey não foi configurado.", "Wompi:PrivateKey est absente de la configuration."),
            ["Falta Wompi:PublicKey en la configuración"] = new("Wompi:PublicKey is missing from the configuration.", "Wompi:PublicKey não foi configurado.", "Wompi:PublicKey est absente de la configuration."),
            ["El tipo de dato (DataType) es obligatorio"] = new("The data type is required.", "O tipo de dado é obrigatório.", "Le type de données est obligatoire."),
            ["El usuario actual no tiene un perfil de empleado asociado."] = new("The current user has no employee profile.", "O usuário atual não tem um perfil de funcionário associado.", "Aucun profil d'employé n'est associé à l'utilisateur actuel."),
            ["El usuario actual no tiene una sede asignada."] = new("The current user has no branch assigned.", "O usuário atual não tem uma unidade atribuída.", "Aucun établissement n'est attribué à l'utilisateur actuel."),
            ["El usuario no tiene un correo registrado"] = new("The user has no email address on file.", "O usuário não tem um e-mail cadastrado.", "Aucune adresse e-mail n'est enregistrée pour cet utilisateur."),
            ["El valor es obligatorio"] = new("The value is required.", "O valor é obrigatório.", "La valeur est obligatoire."),
            ["Empleado no encontrado."] = new("Employee not found.", "Funcionário não encontrado.", "Employé introuvable."),
            ["Error al crear el turno."] = new("Could not create the shift.", "Não foi possível criar o turno.", "Impossible de créer le service."),
            ["Error al crear la asignación de rol."] = new("Could not create the role assignment.", "Não foi possível criar a atribuição do cargo.", "Impossible de créer l'attribution du rôle."),
            ["Error al crear la asignación de permiso."] = new("Could not create the permission assignment.", "Não foi possível criar a atribuição da permissão.", "Impossible de créer l'attribution de l'autorisation."),
            ["Error al obtener el turno activo."] = new("Could not retrieve the active shift.", "Não foi possível obter o turno ativo.", "Impossible de récupérer le service actif."),
            ["Esa relación rol-permiso no existe."] = new("That role-permission assignment does not exist.", "Essa relação entre cargo e permissão não existe.", "Cette association entre rôle et autorisation n'existe pas."),
            ["Esa relación usuario-rol no existe."] = new("That user-role assignment does not exist.", "Essa relação entre usuário e cargo não existe.", "Cette association entre utilisateur et rôle n'existe pas."),
            ["Esta cuenta de empleado ha sido retirada."] = new("This employee account has been deactivated.", "Esta conta de funcionário foi desativada.", "Ce compte d'employé a été désactivé."),
            ["Esta dirección no pertenece al cliente"] = new("This address does not belong to the customer.", "Este endereço não pertence ao cliente.", "Cette adresse n'appartient pas au client."),
            ["Esta dirección ya es la predeterminada"] = new("This address is already the default.", "Este endereço já é o padrão.", "Cette adresse est déjà l'adresse par défaut."),
            ["Esta linea ya fue anulada"] = new("This order item has already been voided.", "Este item do pedido já foi cancelado.", "Cet article de la commande a déjà été annulé."),
            ["Esta línea ya fue marcada como entregada."] = new("This order item has already been marked as delivered.", "Este item do pedido já foi marcado como entregue.", "Cet article de la commande a déjà été marqué comme livré."),
            ["Este link ya no es válido o el turno ha terminado."] = new("This link is no longer valid or the shift has ended.", "Este link não é mais válido ou o turno terminou.", "Ce lien n'est plus valide ou le service est terminé."),
            ["Este pago no te pertenece."] = new("This payment does not belong to you.", "Este pagamento não pertence a você.", "Ce paiement ne vous appartient pas."),
            ["Este pago no tiene factura generada"] = new("No invoice has been generated for this payment.", "Nenhuma fatura foi emitida para este pagamento.", "Aucune facture n'a été générée pour ce paiement."),
            ["Este pago ya fue reembolsado"] = new("This payment has already been refunded.", "Este pagamento já foi reembolsado.", "Ce paiement a déjà été remboursé."),
            ["Este pago ya tiene una factura generada"] = new("An invoice has already been generated for this payment.", "Uma fatura já foi emitida para este pagamento.", "Une facture a déjà été générée pour ce paiement."),
            ["Este pedido no es un domicilio hecho desde la app."] = new("This order is not an app delivery order.", "Este pedido não é uma entrega feita pelo aplicativo.", "Cette commande n'est pas une livraison passée depuis l'application."),
            ["Este pedido no pertenece al cliente"] = new("This order does not belong to the customer.", "Este pedido não pertence ao cliente.", "Cette commande n'appartient pas au client."),
            ["Este pedido no te pertenece."] = new("This order does not belong to you.", "Este pedido não pertence a você.", "Cette commande ne vous appartient pas."),
            ["Este pedido no tiene una dirección de entrega asociada."] = new("This order has no delivery address.", "Este pedido não tem um endereço de entrega associado.", "Aucune adresse de livraison n'est associée à cette commande."),
            ["Este pedido no tiene una reseña"] = new("This order has no review.", "Este pedido não tem uma avaliação.", "Cette commande n'a pas d'avis."),
            ["Este pedido ya está pagado."] = new("This order has already been paid.", "Este pedido já foi pago.", "Cette commande a déjà été payée."),
            ["Este pedido ya fue cancelado"] = new("This order has already been canceled.", "Este pedido já foi cancelado.", "Cette commande a déjà été annulée."),
            ["Este pedido ya fue pagado en su totalidad"] = new("This order has already been paid in full.", "Este pedido já foi pago integralmente.", "Cette commande a déjà été payée en totalité."),
            ["Este pedido ya fue tomado por otro repartidor"] = new("This order has already been claimed by another delivery driver.", "Este pedido já foi aceito por outro entregador.", "Cette commande a déjà été prise en charge par un autre livreur."),
            ["Este pedido ya no admite pago en línea."] = new("This order can no longer be paid online.", "Este pedido não pode mais ser pago on-line.", "Cette commande ne peut plus être payée en ligne."),
            ["Este pedido ya no está pendiente de confirmación."] = new("This order is no longer awaiting confirmation.", "Este pedido não está mais aguardando confirmação.", "Cette commande n'est plus en attente de confirmation."),
            ["Este pedido ya tiene una reseña"] = new("This order already has a review.", "Este pedido já tem uma avaliação.", "Cette commande a déjà un avis."),
            ["Este producto ya está registrado en el menú de esa fecha y franja."] = new("This product is already listed on the menu for that date and time slot.", "Este produto já está no cardápio para essa data e período.", "Ce produit figure déjà au menu pour cette date et ce créneau."),
            ["Firma del webhook inválida."] = new("The webhook signature is invalid.", "A assinatura do webhook é inválida.", "La signature du webhook est invalide."),
            ["La asignación de permiso no existe."] = new("The permission assignment does not exist.", "A atribuição da permissão não existe.", "L'attribution de l'autorisation n'existe pas."),
            ["La calificación debe estar entre 1 y 5"] = new("The rating must be between 1 and 5.", "A avaliação deve estar entre 1 e 5.", "La note doit être comprise entre 1 et 5."),
            ["La calificación debe estar entre 1 y 5."] = new("The rating must be between 1 and 5.", "A avaliação deve estar entre 1 e 5.", "La note doit être comprise entre 1 et 5."),
            ["La clave (Key) es obligatoria"] = new("The key is required.", "A chave é obrigatória.", "La clé est obligatoire."),
            ["La contrasena debe tener al menos 8 caracteres."] = new("The password must contain at least 8 characters.", "A senha deve conter pelo menos 8 caracteres.", "Le mot de passe doit comporter au moins 8 caractères."),
            ["La dirección de la sede es obligatoria."] = new("The branch address is required.", "O endereço da unidade é obrigatório.", "L'adresse de l'établissement est obligatoire."),
            ["La dirección es obligatoria"] = new("The address is required.", "O endereço é obrigatório.", "L'adresse est obligatoire."),
            ["La dirección indicada no existe."] = new("The selected address does not exist.", "O endereço selecionado não existe.", "L'adresse sélectionnée n'existe pas."),
            ["La dirección no existe"] = new("The address does not exist.", "O endereço não existe.", "L'adresse n'existe pas."),
            ["La dirección no pertenece a este cliente."] = new("This address does not belong to this customer.", "Este endereço não pertence a este cliente.", "Cette adresse n'appartient pas à ce client."),
            ["La dirección no puede estar vacía."] = new("The address cannot be empty.", "O endereço não pode ficar vazio.", "L'adresse ne peut pas être vide."),
            ["La factura no existe"] = new("The invoice does not exist.", "A fatura não existe.", "La facture n'existe pas."),
            ["La foto debe ser JPG, PNG o WEBP"] = new("The photo must be a JPG, PNG, or WEBP file.", "A foto deve estar em JPG, PNG ou WEBP.", "La photo doit être au format JPG, PNG ou WEBP."),
            ["La foto no debe superar 5MB"] = new("The photo cannot exceed 5 MB.", "A foto não pode exceder 5 MB.", "La photo ne peut pas dépasser 5 Mo."),
            ["La hora de apertura y la de cierre no pueden ser iguales."] = new("Opening and closing times cannot be the same.", "Os horários de abertura e fechamento não podem ser iguais.", "Les heures d'ouverture et de fermeture ne peuvent pas être identiques."),
            ["La latitud debe estar entre -90 y 90"] = new("Latitude must be between -90 and 90.", "A latitude deve estar entre -90 e 90.", "La latitude doit être comprise entre -90 et 90."),
            ["La linea del pedido no existe"] = new("The order item does not exist.", "O item do pedido não existe.", "L'article de la commande n'existe pas."),
            ["La línea del pedido no existe"] = new("The order item does not exist.", "O item do pedido não existe.", "L'article de la commande n'existe pas."),
            ["La lista de productos es obligatoria."] = new("The product list is required.", "A lista de produtos é obrigatória.", "La liste des produits est obligatoire."),
            ["La lista de productos no puede tener duplicados."] = new("The product list cannot contain duplicates.", "A lista de produtos não pode ter itens duplicados.", "La liste des produits ne peut pas contenir de doublons."),
            ["La longitud debe estar entre -180 y 180"] = new("Longitude must be between -180 and 180.", "A longitude deve estar entre -180 e 180.", "La longitude doit être comprise entre -180 et 180."),
            ["La mesa no existe"] = new("The table does not exist.", "A mesa não existe.", "La table n'existe pas."),
            ["La mesa no existe."] = new("The table does not exist.", "A mesa não existe.", "La table n'existe pas."),
            ["La mesa no pertenece a la sede indicada."] = new("The table does not belong to the selected branch.", "A mesa não pertence à unidade selecionada.", "La table n'appartient pas à l'établissement sélectionné."),
            ["La mesa ya no está disponible. Puede que otro mesero ya la haya tomado."] = new("The table is no longer available. Another waiter may have taken it.", "A mesa não está mais disponível. Outro garçom pode tê-la ocupado.", "La table n'est plus disponible. Un autre serveur l'a peut-être prise."),
            ["La nueva contrasena debe tener al menos 8 caracteres"] = new("The new password must contain at least 8 characters.", "A nova senha deve conter pelo menos 8 caracteres.", "Le nouveau mot de passe doit comporter au moins 8 caractères."),
            ["La pantalla (Screen) es obligatoria"] = new("The screen is required.", "A tela é obrigatória.", "L'écran est obligatoire."),
            ["La reseña no existe"] = new("The review does not exist.", "A avaliação não existe.", "L'avis n'existe pas."),
            ["La sede especificada no existe."] = new("The specified branch does not exist.", "A unidade informada não existe.", "L'établissement indiqué n'existe pas."),
            ["La sede no existe."] = new("The branch does not exist.", "A unidade não existe.", "L'établissement n'existe pas."),
            ["La sede seleccionada no existe"] = new("The selected branch does not exist.", "A unidade selecionada não existe.", "L'établissement sélectionné n'existe pas."),
            ["La sucursal destino no existe."] = new("The destination branch does not exist.", "A unidade de destino não existe.", "L'établissement de destination n'existe pas."),
            ["La tarifa de domicilio no puede ser negativa."] = new("The delivery fee cannot be negative.", "A taxa de entrega não pode ser negativa.", "Les frais de livraison ne peuvent pas être négatifs."),
            ["La zona horaria es obligatoria."] = new("The time zone is required.", "O fuso horário é obrigatório.", "Le fuseau horaire est obligatoire."),
            ["Las contrasenas no coinciden."] = new("The passwords do not match.", "As senhas não coincidem.", "Les mots de passe ne correspondent pas."),
            ["Las notas no pueden superar los 300 caracteres."] = new("Notes cannot exceed 300 characters.", "As observações não podem exceder 300 caracteres.", "Les notes ne peuvent pas dépasser 300 caractères."),
            ["No existe una configuración con esa clave"] = new("No setting exists with that key.", "Não existe uma configuração com essa chave.", "Aucun paramètre n'existe avec cette clé."),
            ["No hay sedes activas disponibles."] = new("There are no active branches available.", "Não há unidades ativas disponíveis.", "Aucun établissement actif n'est disponible."),
            ["No puedes entregar una línea que fue anulada."] = new("You cannot deliver an item that was voided.", "Não é possível entregar um item que foi cancelado.", "Vous ne pouvez pas livrer un article qui a été annulé."),
            ["No puedes entregar una línea que fue cancelada."] = new("You cannot deliver an item that was canceled.", "Não é possível entregar um item cancelado.", "Vous ne pouvez pas livrer un article annulé."),
            ["No se encontró información de perfil para este usuario."] = new("No profile information was found for this user.", "Não foram encontradas informações de perfil para este usuário.", "Aucune information de profil n'a été trouvée pour cet utilisateur."),
            ["No se puede anular una linea ya entregada"] = new("A delivered item cannot be voided.", "Um item entregue não pode ser cancelado.", "Un article livré ne peut pas être annulé."),
            ["No se puede asignar un rol a un usuario que es cliente."] = new("A role cannot be assigned to a customer account.", "Não é possível atribuir um cargo a uma conta de cliente.", "Un rôle ne peut pas être attribué à un compte client."),
            ["No se puede cancelar un pedido ya entregado"] = new("A delivered order cannot be canceled.", "Um pedido entregue não pode ser cancelado.", "Une commande livrée ne peut pas être annulée."),
            ["No se puede cobrar un pedido que aún no ha sido confirmado"] = new("An order cannot be charged before it is confirmed.", "Não é possível cobrar um pedido antes da confirmação.", "Une commande ne peut pas être encaissée avant sa confirmation."),
            ["No se puede eliminar una categoría que tiene productos activos"] = new("A category with active products cannot be deleted.", "Não é possível excluir uma categoria que tenha produtos ativos.", "Une catégorie contenant des produits actifs ne peut pas être supprimée."),
            ["No se puede modificar una linea anulada"] = new("A voided item cannot be modified.", "Um item cancelado não pode ser alterado.", "Un article annulé ne peut pas être modifié."),
            ["No se puede modificar una linea ya entregada"] = new("A delivered item cannot be modified.", "Um item entregue não pode ser alterado.", "Un article livré ne peut pas être modifié."),
            ["No se puede registrar un pago sobre un pedido cancelado"] = new("A payment cannot be recorded for a canceled order.", "Não é possível registrar um pagamento para um pedido cancelado.", "Aucun paiement ne peut être enregistré pour une commande annulée."),
            ["No tienes permiso para activar/desactivar platos ahora mismo."] = new("You do not have permission to activate or deactivate menu items right now.", "Você não tem permissão para ativar ou desativar pratos agora.", "Vous n'avez pas l'autorisation d'activer ou de désactiver des plats pour le moment."),
            ["No tienes permiso para actualizar entregas ahora mismo."] = new("You do not have permission to update deliveries right now.", "Você não tem permissão para atualizar entregas agora.", "Vous n'avez pas l'autorisation de mettre à jour les livraisons pour le moment."),
            ["No tienes permiso para asignar entregas manualmente."] = new("You do not have permission to assign deliveries manually.", "Você não tem permissão para atribuir entregas manualmente.", "Vous n'avez pas l'autorisation d'attribuer des livraisons manuellement."),
            ["No tienes permiso para borrar esta reseña"] = new("You do not have permission to delete this review.", "Você não tem permissão para excluir esta avaliação.", "Vous n'avez pas l'autorisation de supprimer cet avis."),
            ["No tienes permiso para cambiar el estado de este pedido."] = new("You do not have permission to change this order's status.", "Você não tem permissão para alterar o status deste pedido.", "Vous n'avez pas l'autorisation de modifier le statut de cette commande."),
            ["No tienes permiso para cancelar pedidos."] = new("You do not have permission to cancel orders.", "Você não tem permissão para cancelar pedidos.", "Vous n'avez pas l'autorisation d'annuler des commandes."),
            ["No tienes permiso para crear empleados ahora mismo."] = new("You do not have permission to create employees right now.", "Você não tem permissão para cadastrar funcionários agora.", "Vous n'avez pas l'autorisation de créer des employés pour le moment."),
            ["No tienes permiso para eliminar empleados ahora mismo."] = new("You do not have permission to delete employees right now.", "Você não tem permissão para excluir funcionários agora.", "Vous n'avez pas l'autorisation de supprimer des employés pour le moment."),
            ["No tienes permiso para modificar este pedido."] = new("You do not have permission to edit this order.", "Você não tem permissão para alterar este pedido.", "Vous n'avez pas l'autorisation de modifier cette commande."),
            ["No tienes permiso para procesar reembolsos ahora mismo."] = new("You do not have permission to process refunds right now.", "Você não tem permissão para processar reembolsos agora.", "Vous n'avez pas l'autorisation de traiter les remboursements pour le moment."),
            ["No tienes permiso para registrar pagos ahora mismo."] = new("You do not have permission to record payments right now.", "Você não tem permissão para registrar pagamentos agora.", "Vous n'avez pas l'autorisation d'enregistrer des paiements pour le moment."),
            ["No tienes permiso para ver esta dirección"] = new("You do not have permission to view this address.", "Você não tem permissão para visualizar este endereço.", "Vous n'avez pas l'autorisation de consulter cette adresse."),
            ["Producto no encontrado"] = new("Product not found.", "Produto não encontrado.", "Produit introuvable."),
            ["Refresh token invalido o expirado."] = new("The refresh token is invalid or expired.", "O token de atualização é inválido ou expirou.", "Le jeton de renouvellement est invalide ou a expiré."),
            ["Solo el repartidor asignado puede actualizar este domicilio"] = new("Only the assigned delivery driver can update this delivery.", "Somente o entregador atribuído pode atualizar esta entrega.", "Seul le livreur attribué peut mettre à jour cette livraison."),
            ["Solo puedes borrar reseñas de tu propia sede"] = new("You can only delete reviews for your own branch.", "Você só pode excluir avaliações da sua unidade.", "Vous ne pouvez supprimer que les avis de votre établissement."),
            ["Solo puedes calificar cuando el pedido ya fue entregado."] = new("You can only leave a rating after the order has been delivered.", "Você só pode avaliar depois que o pedido for entregue.", "Vous ne pouvez laisser une note qu'après la livraison de la commande."),
            ["Solo puedes editar tu propia reseña"] = new("You can only edit your own review.", "Você só pode editar sua própria avaliação.", "Vous ne pouvez modifier que votre propre avis."),
            ["Solo puedes gestionar el menú de la sede a la que perteneces."] = new("You can only manage the menu for your own branch.", "Você só pode gerenciar o cardápio da sua unidade.", "Vous ne pouvez gérer que le menu de votre établissement."),
            ["Solo puedes gestionar empleados de tu propia sede."] = new("You can only manage employees at your own branch.", "Você só pode gerenciar funcionários da sua unidade.", "Vous ne pouvez gérer que les employés de votre établissement."),
            ["Solo puedes gestionar mesas de la sede a la que perteneces."] = new("You can only manage tables at your own branch.", "Você só pode gerenciar mesas da sua unidade.", "Vous ne pouvez gérer que les tables de votre établissement."),
            ["Solo puedes gestionar turnos de tu misma sede."] = new("You can only manage shifts at your own branch.", "Você só pode gerenciar turnos da sua unidade.", "Vous ne pouvez gérer que les services de votre établissement."),
            ["Solo puedes operar sobre la sede a la que perteneces."] = new("You can only operate at your own branch.", "Você só pode operar na sua unidade.", "Vous ne pouvez travailler que dans votre établissement."),
            ["Solo puedes ver el menú de la sede a la que perteneces."] = new("You can only view the menu for your own branch.", "Você só pode visualizar o cardápio da sua unidade.", "Vous ne pouvez consulter que le menu de votre établissement."),
            ["Solo puedes ver las reseñas de tu propia sede."] = new("You can only view reviews for your own branch.", "Você só pode visualizar avaliações da sua unidade.", "Vous ne pouvez consulter que les avis de votre établissement."),
            ["Solo puedes ver los pedidos de tu propia sede."] = new("You can only view orders for your own branch.", "Você só pode visualizar pedidos da sua unidade.", "Vous ne pouvez consulter que les commandes de votre établissement."),
            ["Solo puedes ver tus propias direcciones"] = new("You can only view your own addresses.", "Você só pode visualizar seus próprios endereços.", "Vous ne pouvez consulter que vos propres adresses."),
            ["Solo se puede facturar un pago completado"] = new("Only completed payments can be invoiced.", "Somente pagamentos concluídos podem ser faturados.", "Seuls les paiements terminés peuvent être facturés."),
            ["Solo se puede tomar repartos de pedidos tipo Delivery"] = new("Only delivery orders can be claimed for delivery.", "Somente pedidos do tipo entrega podem ser aceitos para entrega.", "Seules les commandes de type livraison peuvent être prises en charge."),
            ["Solo se pueden asignar roles a usuarios que son empleados."] = new("Roles can only be assigned to employee accounts.", "Os cargos só podem ser atribuídos a contas de funcionários.", "Les rôles ne peuvent être attribués qu'aux comptes des employés."),
            ["Solo se pueden reembolsar pagos completados"] = new("Only completed payments can be refunded.", "Somente pagamentos concluídos podem ser reembolsados.", "Seuls les paiements terminés peuvent être remboursés."),
            ["Token de recuperacion invalido."] = new("The recovery token is invalid.", "O token de recuperação é inválido.", "Le jeton de récupération est invalide."),
            ["Un pedido a domicilio debe indicar una dirección."] = new("A delivery order must include an address.", "Um pedido para entrega deve informar um endereço.", "Une commande à livrer doit indiquer une adresse."),
            ["Uno o mas de los roles especificados no existen."] = new("One or more of the selected roles do not exist.", "Um ou mais cargos selecionados não existem.", "Un ou plusieurs rôles sélectionnés n'existent pas."),
            ["Usuario no encontrado."] = new("User not found.", "Usuário não encontrado.", "Utilisateur introuvable."),
            ["Ya calificaste a este repartidor en este pedido."] = new("You have already rated this delivery driver for this order.", "Você já avaliou este entregador neste pedido.", "Vous avez déjà évalué ce livreur pour cette commande."),
            ["Ya existe otra categoría con ese nombre"] = new("Another category already has that name.", "Já existe outra categoria com esse nome.", "Une autre catégorie porte déjà ce nom."),
            ["Ya existe otro producto con ese nombre"] = new("Another product already has that name.", "Já existe outro produto com esse nome.", "Un autre produit porte déjà ce nom."),
            ["Ya existe un empleado con ese documento"] = new("An employee with this ID number already exists.", "Já existe um funcionário com este documento.", "Un employé portant ce numéro de pièce d'identité existe déjà."),
            ["Ya existe un producto con ese nombre"] = new("A product with that name already exists.", "Já existe um produto com esse nome.", "Un produit portant ce nom existe déjà."),
            ["Ya existe un usuario registrado con ese correo"] = new("A user with that email is already registered.", "Já existe um usuário cadastrado com esse e-mail.", "Un utilisateur est déjà enregistré avec cette adresse e-mail."),
            ["Ya existe una categoría con ese nombre"] = new("A category with that name already exists.", "Já existe uma categoria com esse nome.", "Une catégorie portant ce nom existe déjà."),
            ["Ya existe una configuración con esa clave para esta sucursal (o global)"] = new("A setting with this key already exists for this branch or globally.", "Já existe uma configuração com esta chave para esta unidade ou globalmente.", "Un paramètre portant cette clé existe déjà pour cet établissement ou globalement."),
            ["Ya existe una mesa con ese número en esta sucursal"] = new("A table with that number already exists at this branch.", "Já existe uma mesa com esse número nesta unidade.", "Une table portant ce numéro existe déjà dans cet établissement."),
            ["Ya existe una preferencia con esa clave para esta pantalla"] = new("A preference with that key already exists for this screen.", "Já existe uma preferência com essa chave para esta tela.", "Une préférence portant cette clé existe déjà pour cet écran."),
            ["Zona horaria inválida (ej. America/Bogota)."] = new("Invalid time zone (for example, America/Bogota).", "Fuso horário inválido (por exemplo, America/Bogota).", "Fuseau horaire invalide (par exemple, America/Bogota)."),
            ["Esta cuenta también está registrada como cliente. Suspéndela para conservar sus datos de cliente."] = new("This account is also registered as a customer. Suspend it to preserve its customer records.", "Esta conta também está cadastrada como cliente. Suspenda-a para preservar os dados do cliente.", "Ce compte est également enregistré comme client. Suspendez-le pour conserver ses données client."),
            ["Esta sede ya tiene un Administrador asignado."] = new("This branch already has an administrator assigned.", "Esta unidade já tem um administrador atribuído.", "Un administrateur est déjà affecté à cet établissement."),
            ["Falta Brevo:ApiKey"] = new("The Brevo API key is missing.", "A chave da API Brevo está ausente.", "La clé API Brevo est manquante."),
            ["Falta EmailSettings:Username"] = new("The email account username is missing.", "O nome de usuário da conta de e-mail está ausente.", "Le nom d'utilisateur du compte e-mail est manquant."),
            ["Finaliza o reasigna los pedidos activos antes de eliminar al empleado."] = new("Complete or reassign active orders before deleting this employee.", "Conclua ou reatribua os pedidos ativos antes de excluir este funcionário.", "Terminez ou réattribuez les commandes actives avant de supprimer cet employé."),
            ["La cantidad máxima de domicilios activos por repartidor debe estar entre 1 y 20."] = new("The maximum number of active deliveries per courier must be between 1 and 20.", "A quantidade máxima de entregas ativas por entregador deve estar entre 1 e 20.", "Le nombre maximal de livraisons actives par livreur doit être compris entre 1 et 20."),
            ["La categoría está inactiva y no se puede agregar al pedido"] = new("This category is inactive and cannot be added to the order.", "Esta categoria está inativa e não pode ser adicionada ao pedido.", "Cette catégorie est inactive et ne peut pas être ajoutée à la commande."),
            ["La contraseña actual es obligatoria."] = new("The current password is required.", "A senha atual é obrigatória.", "Le mot de passe actuel est obligatoire."),
            ["La contraseña actual no es correcta."] = new("The current password is incorrect.", "A senha atual está incorreta.", "Le mot de passe actuel est incorrect."),
            ["La latitud debe estar entre -90 y 90."] = new("Latitude must be between -90 and 90.", "A latitude deve estar entre -90 e 90.", "La latitude doit être comprise entre -90 et 90."),
            ["La longitud debe estar entre -180 y 180."] = new("Longitude must be between -180 and 180.", "A longitude deve estar entre -180 e 180.", "La longitude doit être comprise entre -180 et 180."),
            ["La nueva contraseña debe incluir al menos una letra y un número."] = new("The new password must include at least one letter and one number.", "A nova senha deve incluir pelo menos uma letra e um número.", "Le nouveau mot de passe doit contenir au moins une lettre et un chiffre."),
            ["La nueva contraseña debe ser diferente a la actual."] = new("The new password must be different from the current password.", "A nova senha deve ser diferente da senha atual.", "Le nouveau mot de passe doit être différent du mot de passe actuel."),
            ["La nueva contraseña debe tener al menos 8 caracteres."] = new("The new password must contain at least 8 characters.", "A nova senha deve ter pelo menos 8 caracteres.", "Le nouveau mot de passe doit comporter au moins 8 caractères."),
            ["Las contraseñas no coinciden."] = new("The passwords do not match.", "As senhas não coincidem.", "Les mots de passe ne correspondent pas."),
            ["No puedes eliminar tu propia cuenta."] = new("You cannot delete your own account.", "Você não pode excluir sua própria conta.", "Vous ne pouvez pas supprimer votre propre compte."),
            ["No se encontró el seguimiento de este pedido."] = new("Tracking information for this order was not found.", "Não foram encontradas informações de rastreamento para este pedido.", "Le suivi de cette commande est introuvable."),
            ["No se pudo identificar al repartidor."] = new("Could not identify the delivery driver.", "Não foi possível identificar o entregador.", "Impossible d'identifier le livreur."),
            ["Reasigna o finaliza las entregas activas antes de eliminar al empleado."] = new("Reassign or complete active deliveries before deleting this employee.", "Reatribua ou conclua as entregas ativas antes de excluir este funcionário.", "Réattribuez ou terminez les livraisons actives avant de supprimer cet employé."),
            ["Si la cuenta esta habilitada para recuperacion, recibiras un codigo."] = new("If the account is eligible for recovery, you will receive a code.", "Se a conta estiver habilitada para recuperação, você receberá um código.", "Si le compte peut être récupéré, vous recevrez un code."),
            ["Solo el Gerente puede cambiar la sede de un empleado."] = new("Only the manager can change an employee's branch.", "Somente o gerente pode alterar a unidade de um funcionário.", "Seul le gérant peut changer l'établissement d'un employé."),
            ["Solo un gerente o administrador puede eliminar empleados."] = new("Only a manager or administrator can delete employees.", "Somente um gerente ou administrador pode excluir funcionários.", "Seul un gérant ou un administrateur peut supprimer des employés."),
            ["Solo un repartidor puede enviar ubicación."] = new("Only a delivery driver can send a location.", "Somente um entregador pode enviar uma localização.", "Seul un livreur peut envoyer une position."),
            ["Solo un repartidor puede reportar su ubicación."] = new("Only a delivery driver can report their location.", "Somente um entregador pode informar sua localização.", "Seul un livreur peut signaler sa position."),
            ["Tu sede está desactivada. Contacta al gerente."] = new("Your branch is inactive. Contact the manager.", "Sua unidade está desativada. Entre em contato com o gerente.", "Votre établissement est désactivé. Contactez le gérant."),
            ["Un pedido está listo para despachar."] = new("An order is ready to dispatch.", "Um pedido está pronto para sair.", "Une commande est prête à être expédiée."),
            ["Un pedido está listo para cobrar."] = new("An order is ready for payment.", "Um pedido está pronto para cobrar.", "Une commande est prête à être encaissée."),
            ["Un pedido ya está listo. Puedes iniciar la entrega."] = new("An order is ready. You can start the delivery.", "Um pedido está pronto. Você pode iniciar a entrega.", "Une commande est prête. Vous pouvez commencer la livraison."),
            ["Se te asignó un pedido para entrega."] = new("An order has been assigned to you for delivery.", "Um pedido foi atribuído a você para entrega.", "Une commande vous a été attribuée pour livraison."),
            ["Se agregaron productos al pedido."] = new("Products were added to the order.", "Foram adicionados produtos ao pedido.", "Des produits ont été ajoutés à la commande."),
            ["Hay productos nuevos para cobrar."] = new("There are new items to charge.", "Há novos produtos para cobrar.", "De nouveaux articles sont à encaisser."),
            ["Se cobró un pedido."] = new("An order was paid.", "Um pedido foi cobrado.", "Une commande a été encaissée."),
            ["Tu pago fue aprobado."] = new("Your payment was approved.", "Seu pagamento foi aprovado.", "Votre paiement a été approuvé."),
            ["Tu pedido ya salió hacia tu dirección."] = new("Your order is on its way to your address.", "Seu pedido já está a caminho do seu endereço.", "Votre commande est en route vers votre adresse."),
            ["Tu pedido fue entregado. ¡Buen provecho!"] = new("Your order has been delivered. Enjoy your meal!", "Seu pedido foi entregue. Bom apetite!", "Votre commande a été livrée. Bon appétit !"),
            ["Calificación de un pedido"] = new("Order rating", "Avaliação de um pedido", "Évaluation d'une commande"),
            ["Llegó un pedido a domicilio (pago en efectivo). Falta confirmarlo."] = new("A delivery order has arrived (cash payment). It still needs confirmation.", "Chegou um pedido para entrega (pagamento em dinheiro). Ainda precisa ser confirmado.", "Une commande à livrer est arrivée (paiement en espèces). Elle doit encore être confirmée."),
            ["Llegó un pedido (ya pagado en línea)."] = new("An order has arrived (already paid online).", "Chegou um pedido (já pago on-line).", "Une commande est arrivée (déjà payée en ligne)."),
            ["Llegó un pedido nuevo."] = new("A new order has arrived.", "Chegou um novo pedido.", "Une nouvelle commande est arrivée."),
            ["Un pedido fue cancelado."] = new("An order was canceled.", "Um pedido foi cancelado.", "Une commande a été annulée."),
            ["Mesa disponible"] = new("Table available", "Mesa disponível", "Table disponible"),
            ["Generación de PDF pendiente de implementar. Consulta el TODO en InvoicesController."] = new("PDF generation has not been implemented yet. See the TODO in InvoicesController.", "A geração de PDF ainda não foi implementada. Consulte o TODO em InvoicesController.", "La génération du PDF n'est pas encore implémentée. Consultez le TODO dans InvoicesController."),

            // Títulos de notificaciones almacenadas en español y presentadas por idioma del destinatario.
            ["Pedido nuevo"] = new("New order", "Novo pedido", "Nouvelle commande"),
            ["Pedido actualizado"] = new("Order updated", "Pedido atualizado", "Commande mise à jour"),
            ["Pedido listo"] = new("Order ready", "Pedido pronto", "Commande prête"),
            ["Pedido cancelado"] = new("Order canceled", "Pedido cancelado", "Commande annulée"),
            ["Pedido listo para recoger"] = new("Order ready for pickup", "Pedido pronto para retirada", "Commande prête à récupérer"),
            ["Nuevo domicilio asignado"] = new("New delivery assigned", "Nova entrega atribuída", "Nouvelle livraison attribuée"),
            ["Tu pedido va en camino"] = new("Your order is on the way", "Seu pedido está a caminho", "Votre commande est en route"),
            ["Pedido entregado"] = new("Order delivered", "Pedido entregue", "Commande livrée"),
            ["Pago confirmado"] = new("Payment confirmed", "Pagamento confirmado", "Paiement confirmé"),
            ["Pago registrado"] = new("Payment recorded", "Pagamento registrado", "Paiement enregistré"),
            ["Nueva reseña"] = new("New review", "Nova avaliação", "Nouvel avis"),
            ["Aviso manual"] = new("Manual notice", "Aviso manual", "Notification manuelle"),
            ["Aviso del sistema"] = new("System notice", "Aviso do sistema", "Notification système"),
            ["Nueva calificación"] = new("New rating", "Nova avaliação", "Nouvelle note"),
            ["Turno por terminar"] = new("Shift ending soon", "Turno terminando", "Service bientôt terminé"),
            ["Turno sin programar para mañana"] = new("No shift scheduled for tomorrow", "Nenhum turno programado para amanhã", "Aucun service prévu pour demain"),
            ["El pedido del cliente está pendiente de confirmación en caja. Cocina podrá iniciarlo cuando quede confirmado."] = new("The customer's order is waiting for the cashier to confirm it. The kitchen can start it after confirmation.", "O pedido do cliente aguarda a confirmação do caixa. A cozinha poderá iniciá-lo após a confirmação.", "La commande du client attend la confirmation de la caisse. La cuisine pourra la commencer après confirmation."),
            ["Este pedido fue cancelado. No puedes continuar con la entrega."] = new("This order was canceled. You cannot continue the delivery.", "Este pedido foi cancelado. Você não pode continuar a entrega.", "Cette commande a été annulée. Vous ne pouvez pas poursuivre la livraison."),
            ["Este pedido no tiene una dirección de entrega asociada. Contacta a soporte."] = new("This order has no delivery address. Contact support.", "Este pedido não tem endereço de entrega. Entre em contato com o suporte.", "Cette commande n'a pas d'adresse de livraison. Contactez l'assistance."),
            ["Este pedido ya está pagado en su totalidad y no se puede cancelar."] = new("This order has been paid in full and cannot be canceled.", "Este pedido já foi totalmente pago e não pode ser cancelado.", "Cette commande a été entièrement payée et ne peut pas être annulée."),
            ["Este pedido ya fue pagado por completo. Crea un pedido nuevo para los productos adicionales."] = new("This order has already been paid in full. Create a new order for additional items.", "Este pedido já foi totalmente pago. Crie um novo pedido para os itens adicionais.", "Cette commande a déjà été entièrement payée. Créez une nouvelle commande pour les articles supplémentaires."),
            ["Este pedido ya tiene platos en preparación, listos o entregados."] = new("This order already has items being prepared, ready, or delivered.", "Este pedido já tem itens em preparo, prontos ou entregues.", "Cette commande contient déjà des plats en préparation, prêts ou livrés."),
            ["La dirección es obligatoria: se usa para calcular la ubicación de la sede."] = new("An address is required to calculate the branch location.", "O endereço é obrigatório para calcular a localização da unidade.", "Une adresse est nécessaire pour calculer l'emplacement de l'établissement."),
            ["La entrega al mesero se debe confirmar desde la acción de entrega del pedido."] = new("The handoff to the waiter must be confirmed using the order delivery action.", "A entrega ao garçom deve ser confirmada pela ação de entrega do pedido.", "La remise au serveur doit être confirmée depuis l'action de livraison de la commande."),
            ["La sede aún no tiene configurados el Impoconsumo y la tarifa de domicilio."] = new("This branch has not configured its consumption tax and delivery fee yet.", "Esta unidade ainda não configurou o imposto sobre consumo e a taxa de entrega.", "Cet établissement n'a pas encore configuré la taxe à la consommation et les frais de livraison."),
            ["La sede no tiene hora de apertura y cierre configuradas."] = new("This branch has no opening and closing hours configured.", "Esta unidade não configurou os horários de abertura e fechamento.", "Les horaires d'ouverture et de fermeture de cet établissement ne sont pas configurés."),
            ["La sede seleccionada no está disponible. Elige otra sede e inténtalo de nuevo."] = new("The selected branch is unavailable. Choose another branch and try again.", "A unidade selecionada não está disponível. Escolha outra unidade e tente novamente.", "L'établissement sélectionné n'est pas disponible. Choisissez-en un autre et réessayez."),
            ["La cocina aún no termina este pedido. Espera a que esté listo para salir."] = new("The kitchen has not finished this order yet. Wait until it is ready to go.", "A cozinha ainda não terminou este pedido. Aguarde até que esteja pronto para sair.", "La cuisine n'a pas encore terminé cette commande. Attendez qu'elle soit prête à partir."),
            ["No puedes editar ni eliminar tus propios turnos como Gerente o Administrador."] = new("Managers and administrators cannot edit or delete their own shifts.", "Gerentes e administradores não podem editar nem excluir os próprios turnos.", "Les responsables et les administrateurs ne peuvent pas modifier ni supprimer leurs propres horaires."),
            ["No puedes quitar el rol de Administrador porque es el único de esta sede."] = new("You cannot remove the Administrator role because this is the branch's only administrator.", "Você não pode remover a função de administrador porque esta é a única pessoa com essa função na unidade.", "Vous ne pouvez pas retirer le rôle d'administrateur, car cette personne est la seule de l'établissement à l'avoir."),
            ["No se pudo contactar el servicio de geolocalización. Intenta de nuevo."] = new("The location service could not be reached. Try again.", "Não foi possível acessar o serviço de localização. Tente novamente.", "Le service de géolocalisation est inaccessible. Réessayez."),
            ["No se puede cambiar el tipo de una linea anulada o cancelada"] = new("The type of a voided or canceled item cannot be changed.", "Não é possível alterar o tipo de um item anulado ou cancelado.", "Le type d'un article annulé ne peut pas être modifié."),
            ["No se puede eliminar esta categoría porque tiene productos asociados (incluso inactivos)."] = new("This category cannot be deleted because it has associated products, including inactive ones.", "Esta categoria não pode ser excluída porque tem produtos associados, inclusive inativos.", "Cette catégorie ne peut pas être supprimée, car des produits y sont associés, même s'ils sont inactifs."),
            ["No se puede eliminar este producto porque tiene pedidos asociados."] = new("This product cannot be deleted because it is associated with orders.", "Este produto não pode ser excluído porque está associado a pedidos.", "Ce produit ne peut pas être supprimé, car il est associé à des commandes."),
            ["No se puede eliminar este rol porque hay empleados asignados a él."] = new("This role cannot be deleted because employees are assigned to it.", "Esta função não pode ser excluída porque há funcionários atribuídos a ela.", "Ce rôle ne peut pas être supprimé, car des employés y sont affectés."),
            ["No se puede eliminar una mesa que está Ocupada."] = new("An occupied table cannot be deleted.", "Não é possível excluir uma mesa ocupada.", "Une table occupée ne peut pas être supprimée."),
            ["No se puede liberar la mesa hasta que todos los productos estén entregados al mesero y los pedidos estén pagados."] = new("The table cannot be cleared until all items have been handed to the waiter and all orders have been paid.", "A mesa só pode ser liberada depois que todos os itens forem entregues ao garçom e os pedidos forem pagos.", "La table ne peut être libérée qu'après la remise de tous les articles au serveur et le paiement de toutes les commandes."),
            ["Solo el cajero puede confirmar un pedido realizado por el cliente."] = new("Only the cashier can confirm an order placed by a customer.", "Somente o caixa pode confirmar um pedido feito pelo cliente.", "Seul le caissier peut confirmer une commande passée par un client."),
            ["Solo el Gerente puede activar o desactivar una sede."] = new("Only a manager can activate or deactivate a branch.", "Somente um gerente pode ativar ou desativar uma unidade.", "Seul un responsable peut activer ou désactiver un établissement."),
            ["Solo puedes asignar roles a empleados de tu misma sede."] = new("You can assign roles only to employees at your branch.", "Você só pode atribuir funções a funcionários da sua unidade.", "Vous pouvez attribuer des rôles uniquement aux employés de votre établissement."),
            ["Solo puedes marcar como entregado un producto que cocina ya marcó como listo."] = new("You can mark an item as delivered only after the kitchen marks it ready.", "Você só pode marcar um item como entregue depois que a cozinha o marcar como pronto.", "Vous pouvez marquer un article comme livré uniquement après que la cuisine l'a indiqué comme prêt."),
            ["Solo puedes quitar roles a empleados de tu misma sede."] = new("You can remove roles only from employees at your branch.", "Você só pode remover funções de funcionários da sua unidade.", "Vous pouvez retirer des rôles uniquement aux employés de votre établissement."),
            ["Solo se pueden anular productos que siguen pendientes"] = new("Only pending items can be voided.", "Somente itens pendentes podem ser anulados.", "Seuls les articles en attente peuvent être annulés."),
            ["Solo se pueden calificar pedidos entregados que el cliente hizo por su cuenta desde la app."] = new("Only delivered orders placed by the customer in the app can be rated.", "Só é possível avaliar pedidos entregues que o cliente fez pelo aplicativo.", "Seules les commandes livrées et passées par le client dans l'application peuvent être notées."),
            ["Solo se pueden modificar productos que siguen pendientes"] = new("Only pending items can be changed.", "Somente itens pendentes podem ser alterados.", "Seuls les articles en attente peuvent être modifiés."),
            ["Tu cuenta no tiene una sede asignada todavía. Pide a tu gerente/administrador"] = new("Your account is not assigned to a branch yet. Ask your manager or administrator.", "Sua conta ainda não está vinculada a uma unidade. Fale com seu gerente ou administrador.", "Votre compte n'est pas encore associé à un établissement. Contactez votre responsable ou administrateur."),
            ["Tu pedido ya está en preparación y no se puede cancelar."] = new("Your order is already being prepared and cannot be canceled.", "Seu pedido já está em preparo e não pode ser cancelado.", "Votre commande est déjà en préparation et ne peut pas être annulée."),
            ["Un Administrador no puede asignar el rol de Administrador ni Gerente. Solo un Gerente puede hacerlo."] = new("An Administrator cannot assign the Administrator or Manager role. Only a Manager can do this.", "Um administrador não pode atribuir a função de administrador ou gerente. Somente um gerente pode fazer isso.", "Un administrateur ne peut pas attribuer le rôle d'administrateur ou de responsable. Seul un responsable peut le faire."),
            ["Un Administrador no puede quitar el rol de Administrador ni Gerente. Solo un Gerente puede hacerlo."] = new("An Administrator cannot remove the Administrator or Manager role. Only a Manager can do this.", "Um administrador não pode remover a função de administrador ou gerente. Somente um gerente pode fazer isso.", "Un administrateur ne peut pas retirer le rôle d'administrateur ou de responsable. Seul un responsable peut le faire."),
            ["Este pedido ya tiene platos en preparación, listos o entregados. No se puede cancelar mientras cocina ya empezó a prepararlo."] = new("This order already has items being prepared, ready, or delivered. It cannot be canceled after the kitchen has started preparing it.", "Este pedido já tem itens em preparo, prontos ou entregues. Ele não pode ser cancelado depois que a cozinha começou a prepará-lo.", "Cette commande contient déjà des plats en préparation, prêts ou livrés. Elle ne peut pas être annulée après le début de sa préparation en cuisine."),
            ["La sede aún no tiene configurados el Impoconsumo y la tarifa de domicilio. Pide al administrador que los configure en Ajustes del Restaurante."] = new("This branch has not configured its consumption tax and delivery fee yet. Ask the administrator to set them in Restaurant Settings.", "Esta unidade ainda não configurou o imposto sobre consumo e a taxa de entrega. Peça ao administrador para configurá-los nos ajustes do restaurante.", "La taxe à la consommation et les frais de livraison ne sont pas configurés pour cet établissement. Demandez à l'administrateur de les définir dans les paramètres du restaurant."),
            ["La sede no tiene hora de apertura y cierre configuradas. Configúralas en Configuración > Ajustes del Restaurante antes de crear turnos."] = new("This branch has no opening and closing hours configured. Set them in Settings > Restaurant Settings before creating shifts.", "Esta unidade não configurou os horários de abertura e fechamento. Configure-os em Ajustes > Ajustes do Restaurante antes de criar turnos.", "Les horaires d'ouverture et de fermeture ne sont pas configurés pour cet établissement. Définissez-les dans Paramètres > Paramètres du restaurant avant de créer des horaires."),
            ["No puedes quitar el rol de Administrador porque es el único de esta sede. Asigna otro Administrador antes de quitar este."] = new("You cannot remove the Administrator role because this is the branch's only administrator. Assign another administrator before removing this one.", "Você não pode remover a função de administrador porque esta é a única pessoa com essa função na unidade. Atribua essa função a outra pessoa antes de removê-la daqui.", "Vous ne pouvez pas retirer le rôle d'administrateur, car cette personne est la seule de l'établissement à l'avoir. Attribuez ce rôle à une autre personne avant de le retirer."),
            ["No se puede eliminar esta categoría porque tiene productos asociados (incluso inactivos). Usa la opción de editar para desactivarla (Status) en su lugar."] = new("This category cannot be deleted because it has associated products, including inactive ones. Edit the category to deactivate it instead.", "Esta categoria não pode ser excluída porque tem produtos associados, inclusive inativos. Edite a categoria para desativá-la.", "Cette catégorie ne peut pas être supprimée, car des produits y sont associés, même inactifs. Modifiez-la pour la désactiver."),
            ["No se puede eliminar este producto porque tiene pedidos asociados. Usa Actualizar para desactivarlo en su lugar."] = new("This product cannot be deleted because it is associated with orders. Use Update to deactivate it instead.", "Este produto não pode ser excluído porque está associado a pedidos. Use Atualizar para desativá-lo.", "Ce produit ne peut pas être supprimé, car il est associé à des commandes. Utilisez Modifier pour le désactiver."),
            ["No se puede eliminar este rol porque hay empleados asignados a él. Usa la opción de editar para desactivarlo (Status) en su lugar."] = new("This role cannot be deleted because employees are assigned to it. Edit the role to deactivate it instead.", "Esta função não pode ser excluída porque há funcionários atribuídos a ela. Edite a função para desativá-la.", "Ce rôle ne peut pas être supprimé, car des employés y sont affectés. Modifiez-le pour le désactiver."),
            ["No se puede eliminar una mesa que está Ocupada. Cambia su estado a Disponible primero."] = new("An occupied table cannot be deleted. Change its status to Available first.", "Não é possível excluir uma mesa ocupada. Altere o status para Disponível primeiro.", "Une table occupée ne peut pas être supprimée. Changez d'abord son statut en Disponible."),
            ["Solo se pueden calificar pedidos entregados que el cliente hizo por su cuenta desde la app. Los pedidos tomados por un mesero o cajero en el restaurante no se pueden calificar."] = new("Only delivered orders placed by the customer in the app can be rated. Orders entered by a waiter or cashier at the restaurant cannot be rated.", "Só é possível avaliar pedidos entregues que o cliente fez pelo aplicativo. Pedidos registrados por um garçom ou caixa no restaurante não podem ser avaliados.", "Seules les commandes livrées et passées par le client dans l'application peuvent être notées. Les commandes saisies au restaurant par un serveur ou un caissier ne peuvent pas être notées."),
            ["Tu cuenta no tiene una sede asignada todavía. Pide a tu gerente/administrador que te asigne un turno, y vuelve a iniciar sesión."] = new("Your account is not assigned to a branch yet. Ask your manager or administrator to assign you a shift, then sign in again.", "Sua conta ainda não está vinculada a uma unidade. Peça ao gerente ou administrador para atribuir um turno e entre novamente.", "Votre compte n'est pas encore associé à un établissement. Demandez à votre responsable ou administrateur de vous attribuer un horaire, puis reconnectez-vous."),
        };

    public string ResolveLanguage(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage)) return "es";

        foreach (var candidate in acceptLanguage.Split(','))
        {
            var language = candidate.Split(';', 2)[0].Trim().Split('-', 2)[0].ToLowerInvariant();
            if (language is "es" or "en" or "pt" or "fr") return language;
        }

        return "es";
    }

    public string Localize(string? message, string? language)
    {
        if (string.IsNullOrEmpty(message)) return message ?? string.Empty;
        var locale = ResolveLanguage(language);
        if (locale == "es") return message;

        var text = message.Trim();
        if (Messages.TryGetValue(text, out var translation))
            return PreserveWhitespace(message, Select(translation, locale));

        var dynamic = LocalizeDynamic(text, locale);
        return dynamic is null ? message : PreserveWhitespace(message, dynamic);
    }

    public string LocalizeError(string? message, string? language)
    {
        var locale = ResolveLanguage(language);
        if (string.IsNullOrWhiteSpace(message)) return GenericError(locale);
        if (locale == "es") return message;

        var localized = Localize(message, language);
        return localized == message
            ? GenericError(language)
            : localized;
    }

    public string GenericError(string? language)
    {
        var locale = ResolveLanguage(language);
        return locale switch
        {
            "en" => "The request could not be completed.",
            "pt" => "Não foi possível concluir a solicitação.",
            "fr" => "La demande n'a pas pu être traitée.",
            _ => "No se pudo completar la solicitud."
        };
    }

    public JsonNode? LocalizePayload(object? payload, string? language, bool isErrorResponse)
    {
        if (payload is null) return null;
        var node = JsonSerializer.SerializeToNode(payload, payload.GetType(), PayloadJsonOptions);
        LocalizeNode(node, ResolveLanguage(language), isErrorResponse, inErrors: false);
        return node;
    }

    private void LocalizeNode(JsonNode? node, string language, bool isErrorResponse, bool inErrors)
    {
        if (node is JsonObject obj)
        {
            foreach (var pair in obj.ToList())
            {
                var isMessageField = pair.Key.Equals("message", StringComparison.OrdinalIgnoreCase)
                    || pair.Key.Equals("error", StringComparison.OrdinalIgnoreCase)
                    || pair.Key.Equals("detail", StringComparison.OrdinalIgnoreCase)
                    || pair.Key.Equals("title", StringComparison.OrdinalIgnoreCase);
                var isErrorsField = pair.Key.Equals("errors", StringComparison.OrdinalIgnoreCase);

                if (pair.Value is JsonValue jsonValue
                    && (isMessageField || inErrors)
                    && jsonValue.TryGetValue<string>(out var text)
                    && text is not null)
                {
                    var translated = isErrorResponse
                        ? LocalizeError(text, language)
                        : Localize(text, language);
                    obj[pair.Key] = translated;
                }
                else
                {
                    LocalizeNode(pair.Value, language, isErrorResponse, inErrors || isErrorsField);
                }
            }
        }
        else if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                if (array[index] is JsonValue jsonValue
                    && inErrors
                    && jsonValue.TryGetValue<string>(out var text)
                    && text is not null)
                {
                    array[index] = isErrorResponse
                        ? LocalizeError(text, language)
                        : Localize(text, language);
                }
                else
                {
                    LocalizeNode(array[index], language, isErrorResponse, inErrors);
                }
            }
        }
    }

    private static string? LocalizeDynamic(string text, string language)
    {
        Match match;

        if ((match = Regex.Match(text,
                "^Tu dirección está a (?<km>[\\d.,]+) km de la sede (?<branch>.+) y el radio de entrega es de (?<radius>[\\d.,]+) km\\.\\s*No podemos llevar el pedido a esa dirección\\.$")).Success)
        {
            var km = match.Groups["km"].Value;
            var branch = match.Groups["branch"].Value;
            var radius = match.Groups["radius"].Value;
            return language switch
            {
                "en" => $"Your address is {km} km from {branch}, and the delivery radius is {radius} km. We cannot deliver to that address.",
                "pt" => $"Seu endereço fica a {km} km da unidade {branch}, e o raio de entrega é de {radius} km. Não podemos entregar nesse endereço.",
                "fr" => $"Votre adresse se trouve à {km} km de l'établissement {branch}, et le rayon de livraison est de {radius} km. Nous ne pouvons pas livrer à cette adresse.",
                _ => text
            };
        }

        if ((match = Regex.Match(text, "^(?<field>.+) es obligatoria\\.$")).Success)
        {
            var field = TranslateValidationField(match.Groups["field"].Value, language);
            return language switch
            {
                "en" => $"{field} is required.",
                "pt" => $"{field} é obrigatória.",
                "fr" => $"{field} est obligatoire.",
                _ => text
            };
        }
        if ((match = Regex.Match(text, "^(?<field>.+) debe tener formato HH:mm \\(ej\\. 08:30\\)\\.$")).Success)
        {
            var field = TranslateValidationField(match.Groups["field"].Value, language);
            return language switch
            {
                "en" => $"{field} must use the HH:mm format (for example, 08:30).",
                "pt" => $"{field} deve usar o formato HH:mm (por exemplo, 08:30).",
                "fr" => $"{field} doit respecter le format HH:mm (par exemple, 08:30).",
                _ => text
            };
        }
        if ((match = Regex.Match(text, "^El nombre de la sede no puede superar los (?<max>\\d+) caracteres\\.$")).Success)
            return FormatLengthError(language, "Branch name", "Nome da unidade", "Nom de l'établissement", match.Groups["max"].Value, "maximum");
        if ((match = Regex.Match(text, "^El nombre no debe superar (?<max>\\d+) caracteres$")).Success)
            return FormatLengthError(language, "Name", "Nome", "Nom", match.Groups["max"].Value, "maximum");
        if ((match = Regex.Match(text, "^La descripción no debe superar (?<max>\\d+) caracteres$")).Success)
            return FormatLengthError(language, "Description", "Descrição", "Description", match.Groups["max"].Value, "maximum");
        if ((match = Regex.Match(text, "^La dirección debe tener al menos (?<min>\\d+) caracteres\\.$")).Success)
            return FormatLengthError(language, "Address", "Endereço", "Adresse", match.Groups["min"].Value, "minimum");
        if ((match = Regex.Match(text, "^La dirección debe tener entre (?<min>\\d+) y (?<max>\\d+) caracteres\\.$")).Success)
            return FormatLengthRangeError(language, "Address", "Endereço", "Adresse", match.Groups["min"].Value, match.Groups["max"].Value);
        if ((match = Regex.Match(text, "^La dirección no puede superar los (?<max>\\d+) caracteres\\.$")).Success)
            return FormatLengthError(language, "Address", "Endereço", "Adresse", match.Groups["max"].Value, "maximum");
        if ((match = Regex.Match(text, "^La contraseña debe tener al menos (?<min>\\d+) caracteres\\.$")).Success)
            return FormatLengthError(language, "Password", "Senha", "Mot de passe", match.Groups["min"].Value, "minimum");
        if ((match = Regex.Match(text, "^El nombre '(?<name>.*)' no es válido para un permiso\\.$")).Success)
        {
            var name = match.Groups["name"].Value;
            return language switch
            {
                "en" => $"'{name}' is not a valid permission name.",
                "pt" => $"'{name}' não é um nome de permissão válido.",
                "fr" => $"'{name}' n'est pas un nom d'autorisation valide.",
                _ => text
            };
        }
        if ((match = Regex.Match(text, "^El permiso '(?<name>.*)' ya se encuentra registrado\\.$")).Success)
            return FormatDuplicateName(language, "permission", "permissão", "autorisation", match.Groups["name"].Value);
        if ((match = Regex.Match(text, "^El rol '(?<name>.*)' ya se encuentra registrado\\.$")).Success)
            return FormatDuplicateName(language, "role", "cargo", "rôle", match.Groups["name"].Value);
        if ((match = Regex.Match(text, "^La sede '(?<name>.*)' ya se encuentra registrada\\.$")).Success)
            return FormatDuplicateName(language, "branch", "unidade", "établissement", match.Groups["name"].Value);
        if ((match = Regex.Match(text, "^El usuario '(?<email>.*)' ya tiene asignado el rol '(?<role>.*)'\\.$")).Success)
        {
            var email = match.Groups["email"].Value;
            var role = match.Groups["role"].Value;
            return language switch
            {
                "en" => $"User '{email}' already has the role '{role}'.",
                "pt" => $"O usuário '{email}' já tem o cargo '{role}'.",
                "fr" => $"L'utilisateur '{email}' possède déjà le rôle '{role}'.",
                _ => text
            };
        }
        if ((match = Regex.Match(text, "^El filtro '(?<status>.*)' no es valido\\. Usa: activo, retirado o todos\\.$")).Success)
        {
            var status = TranslateEmployeeStatus(match.Groups["status"].Value, language);
            return language switch
            {
                "en" => $"Filter '{status}' is invalid. Use: active, inactive, or all.",
                "pt" => $"O filtro '{status}' é inválido. Use: ativo, inativo ou todos.",
                "fr" => $"Le filtre '{status}' est invalide. Utilisez : actif, inactif ou tous.",
                _ => text
            };
        }
        if ((match = Regex.Match(text, "^La sede est[áa] cerrada\\. Horario: (?<opening>.+) - (?<closing>.+)\\.$")).Success)
        {
            var opening = match.Groups["opening"].Value;
            var closing = match.Groups["closing"].Value;
            return language switch
            {
                "en" => $"The branch is closed. Hours: {opening}–{closing}.",
                "pt" => $"A unidade está fechada. Horário: {opening}–{closing}.",
                "fr" => $"L'établissement est fermé. Horaires : {opening}–{closing}.",
                _ => text
            };
        }
        if ((match = Regex.Match(text, "^La mesa ya está en estado (?<status>.+)$")).Success)
        {
            var status = TranslateOrderStatus(match.Groups["status"].Value, language);
            return language switch
            {
                "en" => $"The table is already {status}.",
                "pt" => $"A mesa já está {status}.",
                "fr" => $"La table est déjà {status}.",
                _ => text
            };
        }
        if ((match = Regex.Match(text, "^No se puede cambiar el estado de (?<current>.+) a (?<next>.+)$")).Success)
        {
            var current = TranslateOrderStatus(match.Groups["current"].Value, language);
            var next = TranslateOrderStatus(match.Groups["next"].Value, language);
            return language switch
            {
                "en" => $"Status cannot be changed from {current} to {next}.",
                "pt" => $"Não é possível alterar o status de {current} para {next}.",
                "fr" => $"Impossible de changer le statut de {current} à {next}.",
                _ => text
            };
        }
        if ((match = Regex.Match(text, "^(?<prefix>DataType inválido\\. Valores permitidos: |Tema inválido\\. Valores permitidos: )(?<values>.*)$")).Success)
        {
            var isTheme = match.Groups["prefix"].Value.StartsWith("Tema", StringComparison.Ordinal);
            var values = match.Groups["values"].Value;
            return language switch
            {
                "en" => $"Invalid {(isTheme ? "theme" : "data type")}. Allowed values: {values}",
                "pt" => $"{(isTheme ? "Tema" : "Tipo de dado")} inválido. Valores permitidos: {values}",
                "fr" => $"{(isTheme ? "Thème" : "Type de données")} invalide. Valeurs autorisées : {values}",
                _ => text
            };
        }

        if ((match = Regex.Match(text, "^Llegó el pedido #(?<id>\\d+)\\.$")).Success)
            return FormatOrder(match, language, "Order #{id} has arrived.", "O pedido #{id} chegou.", "La commande n°{id} est arrivée.");
        if ((match = Regex.Match(text, "^El pedido #(?<id>\\d+) está listo para despachar\\.$")).Success)
            return FormatOrder(match, language, "Order #{id} is ready to dispatch.", "O pedido #{id} está pronto para sair.", "La commande n°{id} est prête à être expédiée.");
        if ((match = Regex.Match(text, "^Llegó el pedido #(?<id>\\d+) a domicilio \\(pago en efectivo\\)\\. Falta confirmarlo\\.$")).Success)
            return FormatOrder(match, language, "Delivery order #{id} has arrived (cash payment). It still needs confirmation.", "O pedido para entrega #{id} chegou (pagamento em dinheiro). Ainda precisa ser confirmado.", "La commande à livrer n°{id} est arrivée (paiement en espèces). Elle doit encore être confirmée.");
        if ((match = Regex.Match(text, "^El pedido #(?<id>\\d+) fue cancelado\\.$")).Success)
            return FormatOrder(match, language, "Order #{id} was canceled.", "O pedido #{id} foi cancelado.", "La commande n°{id} a été annulée.");
        if ((match = Regex.Match(text, "^Se agregaron productos al pedido #(?<id>\\d+)\\.$")).Success)
            return FormatOrder(match, language, "Products were added to order #{id}.", "Foram adicionados produtos ao pedido #{id}.", "Des produits ont été ajoutés à la commande n°{id}.");
        if ((match = Regex.Match(text, "^Pedido #(?<id>\\d+): hay productos nuevos para cobrar\\.$")).Success)
            return FormatOrder(match, language, "Order #{id}: new items are ready to be charged.", "Pedido #{id}: há novos produtos para cobrar.", "Commande n°{id} : de nouveaux articles sont à encaisser.");
        if ((match = Regex.Match(text, "^El pedido #(?<id>\\d+) está listo para cobrar\\.$")).Success)
            return FormatOrder(match, language, "Order #{id} is ready for payment.", "O pedido #{id} está pronto para cobrar.", "La commande n°{id} est prête à être encaissée.");
        if ((match = Regex.Match(text, "^Se te asignó el pedido #(?<id>\\d+) para entrega\\.$")).Success)
            return FormatOrder(match, language, "Order #{id} has been assigned to you for delivery.", "O pedido #{id} foi atribuído a você para entrega.", "La commande n°{id} vous a été attribuée pour livraison.");
        if ((match = Regex.Match(text, "^El pedido #(?<id>\\d+) ya está listo\\. Puedes iniciar la entrega\\.$")).Success)
            return FormatOrder(match, language, "Order #{id} is ready. You can start the delivery.", "O pedido #{id} está pronto. Você pode iniciar a entrega.", "La commande n°{id} est prête. Vous pouvez commencer la livraison.");
        if ((match = Regex.Match(text, "^Tu pago para el pedido #(?<id>\\d+) fue aprobado\\.$")).Success)
            return FormatOrder(match, language, "Your payment for order #{id} was approved.", "Seu pagamento do pedido #{id} foi aprovado.", "Votre paiement pour la commande n°{id} a été approuvé.");
        if ((match = Regex.Match(text, "^Tu pedido #(?<id>\\d+) ya salió hacia tu dirección\\.$")).Success)
            return FormatOrder(match, language, "Your order #{id} is on its way to your address.", "Seu pedido #{id} já está a caminho do seu endereço.", "Votre commande n°{id} est en route vers votre adresse.");
        if ((match = Regex.Match(text, "^Tu pedido #(?<id>\\d+) fue entregado\\. ¡Buen provecho!$")).Success)
            return FormatOrder(match, language, "Your order #{id} has been delivered. Enjoy your meal!", "Seu pedido #{id} foi entregue. Bom apetite!", "Votre commande n°{id} a été livrée. Bon appétit !");
        if ((match = Regex.Match(text, "^Se cobró el pedido #(?<id>\\d+)\\.$")).Success)
            return FormatOrder(match, language, "Payment was collected for order #{id}.", "O pagamento do pedido #{id} foi recebido.", "Le paiement de la commande n°{id} a été encaissé.");
        if ((match = Regex.Match(text, "^Calificación del pedido #(?<id>\\d+)$")).Success)
            return FormatOrder(match, language, "Rating for order #{id}", "Avaliação do pedido #{id}", "Note de la commande n°{id}");
        if ((match = Regex.Match(text, "^Se registró un pago de (?<amount>.+) para el pedido #(?<id>\\d+)\\.$")).Success)
        {
            var amount = match.Groups["amount"].Value;
            var id = match.Groups["id"].Value;
            return language switch
            {
                "en" => $"A payment of {amount} was recorded for order #{id}.",
                "pt" => $"Foi registrado um pagamento de {amount} para o pedido #{id}.",
                "fr" => $"Un paiement de {amount} a été enregistré pour la commande n°{id}.",
                _ => text
            };
        }
        if ((match = Regex.Match(text, "^Se registró un pago de (?<amount>.+)\\.$")).Success)
        {
            var amount = match.Groups["amount"].Value;
            return language switch
            {
                "en" => $"A payment of {amount} was recorded.",
                "pt" => $"Foi registrado um pagamento de {amount}.",
                "fr" => $"Un paiement de {amount} a été enregistré.",
                _ => text
            };
        }

        if ((match = Regex.Match(text, "^Llegó el pedido #(?<id>\\d+) \\(ya pagado en línea\\)\\.$")).Success)
            return FormatOrder(match, language, "Order #{id} arrived (already paid online).", "O pedido #{id} chegou (já pago on-line).", "La commande n°{id} est arrivée (déjà payée en ligne).");

        if ((match = Regex.Match(text, "^(.+?) calificó el pedido #(?<id>\\d+) con (?<score>\\d+)/5\\.(?: Comentario: (?<comment>.*))?$", RegexOptions.Singleline)).Success)
        {
            var person = match.Groups[1].Value;
            var id = match.Groups["id"].Value;
            var score = match.Groups["score"].Value;
            var comment = match.Groups["comment"].Success ? $" {SelectText(language, "Comment:", "Comentário:", "Commentaire :")} {match.Groups["comment"].Value}" : string.Empty;
            return language switch
            {
                "en" => $"{person} rated order #{id} {score}/5.{comment}",
                "pt" => $"{person} avaliou o pedido #{id} com {score}/5.{comment}",
                "fr" => $"{person} a noté la commande n°{id} {score}/5.{comment}",
                _ => text
            };
        }

        if ((match = Regex.Match(text, "^(?<customer>.+?) calificó un pedido con (?<score>[^/]+)/5\\.(?: Comentario: (?<comment>.*))?$", RegexOptions.Singleline)).Success)
        {
            var customer = match.Groups["customer"].Value;
            var score = match.Groups["score"].Value;
            var comment = match.Groups["comment"].Success
                ? $" {SelectText(language, "Comment:", "Comentário:", "Commentaire :")} {match.Groups["comment"].Value}"
                : string.Empty;
            return language switch
            {
                "en" => $"{customer} rated an order {score}/5.{comment}",
                "pt" => $"{customer} avaliou um pedido com {score}/5.{comment}",
                "fr" => $"{customer} a évalué une commande {score}/5.{comment}",
                _ => text
            };
        }

        if ((match = Regex.Match(text, "^Cliente: (?<customer>.*)\\nRepartidor: (?<courier>.*)\\nCalificación: (?<score>[^/]+)/5(?:\\nComentario: (?<comment>.*))?$", RegexOptions.Singleline)).Success)
        {
            var customer = match.Groups["customer"].Value;
            var courier = match.Groups["courier"].Value;
            var score = match.Groups["score"].Value;
            var result = language switch
            {
                "en" => $"Customer: {customer}\nDelivery driver: {courier}\nRating: {score}/5",
                "pt" => $"Cliente: {customer}\nEntregador: {courier}\nAvaliação: {score}/5",
                "fr" => $"Client : {customer}\nLivreur : {courier}\nNote : {score}/5",
                _ => text
            };
            if (match.Groups["comment"].Success)
                result += $"\n{SelectText(language, "Comment:", "Comentário:", "Commentaire :")} {match.Groups["comment"].Value}";
            return result;
        }

        if ((match = Regex.Match(text, "^(?<place>.+): el pedido está listo para servir\\.$")).Success)
        {
            var place = match.Groups["place"].Value;
            if (place.Equals("Para llevar", StringComparison.OrdinalIgnoreCase))
                place = SelectText(language, "To go", "Para levar", "À emporter");
            return language switch
            {
                "en" => $"{place}: the order is ready to serve.",
                "pt" => $"{place}: o pedido está pronto para servir.",
                "fr" => $"{place} : la commande est prête à être servie.",
                _ => text
            };
        }

        if ((match = Regex.Match(text, "^La mesa (?<number>.+) quedó disponible: el pedido fue entregado y pagado\\.$")).Success)
        {
            var number = match.Groups["number"].Value;
            return language switch
            {
                "en" => $"Table {number} is available: the order was delivered and paid.",
                "pt" => $"A mesa {number} está disponível: o pedido foi entregue e pago.",
                "fr" => $"La table {number} est disponible : la commande a été livrée et payée.",
                _ => text
            };
        }

        if ((match = Regex.Match(text, "^El turno de (?<employee>.+) \\((?<role>.*)\\) termina hoy y no tiene turno asignado para mañana\\. Si no defines uno nuevo, se repetira el mismo turno automaticamente\\.$")).Success)
        {
            var employee = match.Groups["employee"].Value;
            var role = match.Groups["role"].Value;
            return language switch
            {
                "en" => $"{employee}'s ({role}) shift ends today, and no shift is scheduled for tomorrow. If you do not assign one, today's shift will repeat automatically.",
                "pt" => $"O turno de {employee} ({role}) termina hoje e não há turno programado para amanhã. Se você não atribuir outro, o turno de hoje será repetido automaticamente.",
                "fr" => $"Le service de {employee} ({role}) se termine aujourd'hui et aucun service n'est prévu demain. Si vous n'en attribuez pas un, le service d'aujourd'hui sera répété automatiquement.",
                _ => text
            };
        }

        if ((match = Regex.Match(text, "^Empleado registrado correctamente\\. Se envio un codigo de activacion a (?<email>.+) para que confirme su cuenta\\.$")).Success)
        {
            var email = match.Groups["email"].Value;
            return language switch
            {
                "en" => $"Employee registered. An activation code was sent to {email} to confirm the account.",
                "pt" => $"Funcionário cadastrado. Um código de ativação foi enviado para {email} para confirmar a conta.",
                "fr" => $"Employé enregistré. Un code d'activation a été envoyé à {email} pour confirmer le compte.",
                _ => text
            };
        }

        return null;
    }

    private static string TranslateValidationField(string field, string language)
    {
        var labels = new Dictionary<string, (string English, string Portuguese, string French)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Correo"] = ("Email", "E-mail", "E-mail"),
            ["Correo electrónico"] = ("Email", "E-mail", "E-mail"),
            ["Nombre"] = ("Name", "Nome", "Nom"),
            ["Apellido"] = ("Last name", "Sobrenome", "Nom de famille"),
            ["Documento"] = ("ID number", "Documento", "Numéro de pièce d'identité"),
            ["Teléfono"] = ("Phone number", "Telefone", "Téléphone"),
            ["Dirección"] = ("Address", "Endereço", "Adresse"),
            ["Fecha"] = ("Date", "Data", "Date"),
            ["Hora"] = ("Time", "Horário", "Heure"),
            ["Contraseña"] = ("Password", "Senha", "Mot de passe")
        };
        if (!labels.TryGetValue(field, out var translation)) return field;
        return SelectText(language, translation.English, translation.Portuguese, translation.French);
    }

    private static string TranslateEmployeeStatus(string status, string language) => status.ToLowerInvariant() switch
    {
        "activo" => SelectText(language, "active", "ativo", "actif"),
        "retirado" => SelectText(language, "inactive", "inativo", "inactif"),
        "todos" => SelectText(language, "all", "todos", "tous"),
        _ => status
    };

    private static string TranslateOrderStatus(string status, string language) => status.ToLowerInvariant() switch
    {
        "disponible" => SelectText(language, "available", "disponível", "disponible"),
        "ocupada" => SelectText(language, "occupied", "ocupada", "occupée"),
        "pendiente" => SelectText(language, "pending", "pendente", "en attente"),
        "confirmado" => SelectText(language, "confirmed", "confirmado", "confirmée"),
        "en preparación" => SelectText(language, "being prepared", "em preparo", "en préparation"),
        "listo" => SelectText(language, "ready", "pronto", "prête"),
        "entregado" => SelectText(language, "delivered", "entregue", "livrée"),
        "cancelado" => SelectText(language, "canceled", "cancelado", "annulée"),
        _ => status
    };

    private static string FormatLengthError(string language, string englishField, string portugueseField, string frenchField, string length, string kind)
    {
        var field = SelectText(language, englishField, portugueseField, frenchField);
        return language switch
        {
            "en" when kind == "minimum" => $"{field} must contain at least {length} characters.",
            "en" => $"{field} cannot exceed {length} characters.",
            "pt" when kind == "minimum" => $"{field} deve ter pelo menos {length} caracteres.",
            "pt" => $"{field} não pode exceder {length} caracteres.",
            "fr" when kind == "minimum" => $"{field} doit comporter au moins {length} caractères.",
            _ => $"{field} ne peut pas dépasser {length} caractères."
        };
    }

    private static string FormatLengthRangeError(string language, string englishField, string portugueseField, string frenchField, string minimum, string maximum)
    {
        var field = SelectText(language, englishField, portugueseField, frenchField);
        return language switch
        {
            "en" => $"{field} must contain between {minimum} and {maximum} characters.",
            "pt" => $"{field} deve ter entre {minimum} e {maximum} caracteres.",
            "fr" => $"{field} doit comporter entre {minimum} et {maximum} caractères.",
            _ => $"{field} doit comporter entre {minimum} et {maximum} caractères."
        };
    }

    private static string FormatDuplicateName(string language, string englishType, string portugueseType, string frenchType, string name) => language switch
    {
        "en" => $"The {englishType} name '{name}' is already registered.",
        "pt" => $"Este nome já está cadastrado para {portugueseType}: '{name}'.",
        "fr" => $"Le nom « {name} » est déjà utilisé pour {frenchType}.",
        _ => name
    };

    private static string FormatOrder(Match match, string language, string english, string portuguese, string french)
    {
        var template = SelectText(language, english, portuguese, french);
        return template.Replace("{id}", match.Groups["id"].Value, StringComparison.Ordinal);
    }

    private static string Select(LocalizedMessage translation, string language) => language switch
    {
        "en" => translation.English,
        "pt" => translation.Portuguese,
        "fr" => translation.French,
        _ => string.Empty
    };

    private static string SelectText(string language, string english, string portuguese, string french) => language switch
    {
        "en" => english,
        "pt" => portuguese,
        "fr" => french,
        _ => string.Empty
    };

    private static string PreserveWhitespace(string original, string localized)
    {
        var leading = original.Length - original.TrimStart().Length;
        var trailing = original.Length - original.TrimEnd().Length;
        return original[..leading] + localized + (trailing == 0 ? string.Empty : original[^trailing..]);
    }
}
